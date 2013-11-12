using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using FastCgiNet;
using Fos.Logging;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Fos.Listener
{
	internal delegate void ReceiveStdinRecord(FosRequest req, StdinRecord record);
	internal delegate void ReceiveStdoutRecord(FosRequest req, StdoutRecord record);
	internal delegate void ReceiveStderrRecord(FosRequest req, StderrRecord record);
	internal delegate void ReceiveBeginRequestRecord(FosRequest req, BeginRequestRecord record);
	internal delegate void ReceiveEndRequestRecord(FosRequest req, EndRequestRecord record);
	internal delegate void ReceiveParamsRecord(FosRequest req, ParamsRecord record);

	/// <summary>
	/// This class will provide you a listener application. It will listen to new connections and handle data while
	/// providing ways for you to know what records different FastCgi connections are receiving. It features a highly asynchronous
	/// main loop to make things run fast!
	/// </summary>
	internal class SocketListener : IDisposable
	{
		private const int listenBacklog = 500;
		private Socket tcpListenSocket;
		private Socket unixListenSocket;
		private bool SomeListenSocketHasBeenBound
		{
			get
			{
				return (tcpListenSocket != null && tcpListenSocket.IsBound) || ( unixListenSocket != null && unixListenSocket.IsBound);
			}
		}
		private IServerLogger Logger;
		private RecordFactory RecFactory = new RecordFactory();

		public bool IsRunning { get; private set; }

		/// <summary>
		/// Requests indexed by their sockets.
		/// </summary>
		private ConcurrentDictionary<Socket, ByteReaderAndRequest> OpenSockets = new ConcurrentDictionary<Socket, ByteReaderAndRequest>();

		#region Events to receive records
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveBeginRequestRecord OnReceiveBeginRequestRecord = delegate {};
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveParamsRecord OnReceiveParamsRecord = delegate {};
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveStdinRecord OnReceiveStdinRecord = delegate {};
		/// <summary>
		/// Upon receiving a record with this event, do not run any blocking code, or the application's main loop will block as well.
		/// </summary>
		public event ReceiveStdoutRecord OnReceiveStdoutRecord = delegate {};
		#endregion

		/// <summary>
		/// Closes and disposes of a Request and its Socket while also removing it from the internal collection of open sockets.
		/// </summary>
		private void OnSocketClose(Request req, bool abrupt)
		{
			ByteReaderAndRequest trash;
			OpenSockets.TryRemove(req.Socket, out trash);

			if (Logger != null)
			{
				if (abrupt)
					Logger.LogConnectionClosedAbruptly(req.Socket);
				else
					Logger.LogConnectionEndedNormally(req.Socket);
			}
		}

		private void Work()
		{
			byte[] buffer = new byte[8192];
			int selectMaximumTime = -1;
			while (true)
			{
				try
				{
					// We want Select to return when either a new connection has arrived or when there is incoming socket data
					List<Socket> socketsReadSet = OpenSockets.Keys.ToList();
					if (tcpListenSocket != null && tcpListenSocket.IsBound)
						socketsReadSet.Add(tcpListenSocket);
					if (unixListenSocket != null && unixListenSocket.IsBound)
						socketsReadSet.Add(unixListenSocket);

					Socket.Select(socketsReadSet, null, null, selectMaximumTime);
					
					foreach (Socket sock in socketsReadSet)
					{
						// In case this sock is in the read set just because a connection has been accepted,
						// it must be a listen socket and we should accept the new queued connections
						if (sock == tcpListenSocket || sock == unixListenSocket)
						{
							BeginAcceptNewConnections(sock);
							continue;
						}

						// Read from the socket
						var brr = OpenSockets[sock];
						var fcgiRequest = brr.FCgiRequest;
						var fosRequest = brr.FosRequest;
						var byteReader = brr.ByteReader;

						int bytesRead = sock.Receive(buffer, SocketFlags.None);

						// Feed the byte reader and signal our events
						// Catch application errors to avoid service disruption
						try
						{
							bool abortRequest = false;
							foreach (var builtRecord in byteReader.Read(buffer, 0, bytesRead))
							{
								if (abortRequest)
								{
									fcgiRequest.CloseSocket();
									break;
								}

								switch (builtRecord.RecordType)
								{
									case RecordType.FCGIBeginRequest:
										// We need this record to create our special FosRequest!
										var beginRec = (BeginRequestRecord) builtRecord;
										fosRequest = new FosRequest(fcgiRequest, beginRec, Logger);
										brr.FosRequest = fosRequest;
										fcgiRequest.SetBeginRequest(beginRec);
										OnReceiveBeginRequestRecord(brr.FosRequest, beginRec);
										break;
									
									case RecordType.FCGIParams:
										OnReceiveParamsRecord(fosRequest, (ParamsRecord)builtRecord);
										break;
									
									case RecordType.FCGIStdin:
										OnReceiveStdinRecord(fosRequest, (StdinRecord)builtRecord);
										break;

									case RecordType.FCGIStdout:
										OnReceiveStdoutRecord(fosRequest, (StdoutRecord)builtRecord);
										break;
									
									default:
										if (Logger != null)
											Logger.LogInvalidRecordReceived(builtRecord);
										abortRequest = true;
										break;
								}
							}
						}
						catch (Exception e)
						{
							// Log and end request
							if (Logger != null)
								Logger.LogServerError(e, "An internal event handler did not handle an exception thrown by the application");

							fcgiRequest.CloseSocket();
							continue;
						}
					}
				}
				catch (Exception e)
				{
					if (Logger != null)
						Logger.LogServerError(e, "Exception would end the data receiving loop. This is extremely bad. Please file a bug report.");
				}
			}
		}

		/*
		void OnConnectionAccepted(object sender, SocketAsyncEventArgs e)
		{
			var brr = new ByteReaderAndRequest(new ByteReader(RecFactory));
			OpenSockets[Socket] = brr;
		}
		*/

		/// <summary>
		/// Accepts all pending connections on a socket asynchronously.
		/// </summary>
		void BeginAcceptNewConnections(Socket listenSocket)
		{
			Socket newConnection;
			try
			{
				// The commented implementation crashes Mono with a too many heaps warning on Mono 3.0.7... investigate later
				/*
				SocketAsyncEventArgs args;
				do
				{
					args = new SocketAsyncEventArgs();
					args.Completed += OnConnectionAccepted;
				}
				while (listenSocket.AcceptAsync(args) == true);*/

				newConnection = listenSocket.Accept();
				var request = new Request(newConnection);
				request.OnSocketClose += OnSocketClose;
				OpenSockets[newConnection] = new ByteReaderAndRequest(new ByteReader(RecFactory), request);
				if (Logger != null)
					Logger.LogConnectionReceived(newConnection);
			}
			catch (Exception e)
			{
				//TODO: Handle it
			}
		}

		/// <summary>
		/// Set this to an <see cref="Fos.Logging.IServerLogger"/> to log server information.
		/// </summary>
		public void SetLogger(IServerLogger logger)
		{
			if (logger == null)
				throw new ArgumentNullException("logger");

			this.Logger = logger;
		}

		/// <summary>
		/// Defines on what address and what port the TCP socket will listen on.
		/// </summary>
		public void Bind(IPAddress addr, int port)
		{
			tcpListenSocket.Bind(new IPEndPoint(addr, port));
		}

#if __MonoCS__
		/// <summary>
		/// Defines the unix socket path to listen on.
		/// </summary>
		public void Bind(string socketPath)
		{
			var endpoint = new Mono.Unix.UnixEndPoint(socketPath);
			unixListenSocket.Bind (endpoint);
		}
#endif

		/// <summary>
		/// Start this FastCgi application. This method blocks while the program runs.
		/// </summary>
		public void Start() {
			if (!SomeListenSocketHasBeenBound)
				throw new InvalidOperationException("You have to bind to some address or unix socket file first");

			if (tcpListenSocket != null && tcpListenSocket.IsBound)
				tcpListenSocket.Listen(listenBacklog);
			if (unixListenSocket != null && unixListenSocket.IsBound)
				unixListenSocket.Listen(listenBacklog);

			// Wait for connections without blocking
			IsRunning = true;

			// Block here
			Work();
		}

		/// <summary>
		/// Start this FastCgi application. This method does not block and only returns when the server is ready to accept connections.
		/// </summary>
		public void StartInBackground() {
			if (!SomeListenSocketHasBeenBound)
				throw new InvalidOperationException("You have to bind to some address or unix socket file first");

			if (tcpListenSocket != null && tcpListenSocket.IsBound)
				tcpListenSocket.Listen(listenBacklog);
			if (unixListenSocket != null && unixListenSocket.IsBound)
				unixListenSocket.Listen(listenBacklog);

			// Set this before waiting for connections
			IsRunning = true;

			//TODO: If one of the tasks below is delayed (why in the world would that happen, idk) then this
			// method returns without being ready to accept connections..
			Task.Factory.StartNew(Work);
		}

		/// <summary>
		/// Closes the listen socket and all active connections abruptly.
		/// </summary>
		public void Stop()
		{
			IsRunning = false;

			if (tcpListenSocket != null && tcpListenSocket.IsBound)
				tcpListenSocket.Close();
			if (unixListenSocket != null && unixListenSocket.IsBound)
				unixListenSocket.Close();

			//TODO: Stop task that waits for connection data..

			foreach (var socketAndRequest in OpenSockets)
			{
				socketAndRequest.Value.FCgiRequest.CloseSocket();
			}
		}

		/// <summary>
		/// Stops the server if it hasn't been stopped and disposes of resources, including a logger if one has been set.
		/// </summary>
		public void Dispose()
		{
			Stop();

			if (tcpListenSocket != null)
				tcpListenSocket.Dispose();
			if (unixListenSocket != null)
				unixListenSocket.Dispose();

			if (Logger != null)
			{
				var disposableLogger = Logger as IDisposable;
				if (disposableLogger != null)
					disposableLogger.Dispose();
			}
		}

		public SocketListener ()
		{
			tcpListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
#if __MonoCs__
			unixListenSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP); // man unix (7) says most unix implementations are safe on deliveryand order
#endif

			IsRunning = false;
		}
	}
}
