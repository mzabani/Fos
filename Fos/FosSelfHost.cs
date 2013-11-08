using System;
using FastCgiNet;
using FastCgiNet.Logging;
using Fos.Owin;
using Owin;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace Fos
{
	/// <summary>
	/// This is the class you need to use to host your web application. It will create a TCP Socket that receives FastCgi
	/// connections from your web server, passing them to the Owin pipeline.
	/// </summary>
	public class FosSelfHost : IDisposable
	{
		private FastCgiHostApplication fastCgiProgram;
		private FCgiAppBuilder AppBuilder;
		private ConcurrentDictionary<Request, FCgiRequest> requestsStatuses;
		private Func<IDictionary<string, object>, System.Threading.Tasks.Task> OwinPipelineEntry;
		private Action<IAppBuilder> ApplicationConfigure;
		private ILogger logger;
		private CancellationTokenSource onAppDisposal;

		/// <summary>
		/// Starts this FastCgi server! This method only returns when the server is ready to accept connections.
		/// </summary>
		/// <param name="background">True if this method starts the server without blocking, false to block.</param>
		public void Start(bool background)
		{
			AppBuilder = new FCgiAppBuilder(onAppDisposal.Token);

			// Configure the application and build our pipeline entry
			ApplicationConfigure(AppBuilder);
			OwinPipelineEntry = (Func<IDictionary<string, object>, System.Threading.Tasks.Task>) AppBuilder.Build(typeof(Func<IDictionary<string, object>, System.Threading.Tasks.Task>));

			fastCgiProgram.OnReceiveBeginRequestRecord += OnReceiveBeginRequest;
			fastCgiProgram.OnReceiveParamsRecord += OnReceiveParams;
			fastCgiProgram.OnReceiveStdinRecord += OnReceiveStdin;

			if (logger != null)
				fastCgiProgram.SetLogger(logger);

			if (!background)
				fastCgiProgram.Start();
			else
				fastCgiProgram.StartInBackground();
		}

		/// <summary>
		/// Notifies the application (in non-standard fashion) that the server is stopping and stops listening for connections, while
		/// closing active connections abruptely.
		/// </summary>
		public void Stop()
		{
			// Tell the application the server is disposing
			onAppDisposal.Cancel();
			onAppDisposal.Dispose();

			fastCgiProgram.Stop();
		}

		public void SetLogger(ILogger logger)
		{
			this.logger = logger;
		}

		void OnReceiveBeginRequest(Request req, BeginRequestRecord rec)
		{
			requestsStatuses[req] = new FCgiRequest(req, rec, OwinPipelineEntry, logger);
		}

		void OnReceiveParams(Request req, ParamsRecord rec) {
			requestsStatuses[req].ReceiveParams(rec);
		}

		void OnReceiveStdin(Request req, StdinRecord rec) {
			var onCloseConnection = requestsStatuses[req].ReceiveStdin(rec);
			if (onCloseConnection == null)
				return;

			onCloseConnection.ContinueWith(t =>
			{
				//TODO: The task may have failed or something else happened, verify
				FCgiRequest trash;
				requestsStatuses.TryRemove(req, out trash);
				//TODO: Log failure to remove request from dictionary
				if (t.Result)
					req.CloseSocket();
			});
		}

		/// <summary>
		/// Binds the TCP listen socket to an address an a port.
		/// </summary>
		public void Bind(System.Net.IPAddress addr, int port)
		{
			fastCgiProgram.Bind(addr, port);
		}

#if __MonoCS__
		/// <summary>
		/// Defines the unix socket path to listen on.
		/// </summary>
		public void Bind(string socketPath)
		{
			fastCgiProgram.Bind(socketPath);
		}
#endif

		public void Dispose()
		{
			Stop();
			onAppDisposal.Dispose();
			fastCgiProgram.Dispose();
		}

		/// <summary>
		/// This constructor lets you specify the method to be executed to register your application's middleware.
		/// </summary>
		public FosSelfHost(Action<IAppBuilder> configureMethod)
		{
			ApplicationConfigure = configureMethod;
			requestsStatuses = new ConcurrentDictionary<Request, FCgiRequest>();
			fastCgiProgram = new FastCgiHostApplication();
			onAppDisposal = new CancellationTokenSource();
		}
	}
}
