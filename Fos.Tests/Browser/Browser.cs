using System;
using FastCgiNet;
using System.Net;
using System.Net.Sockets;

namespace Fos.Tests
{
	/// <summary>
	/// This class helps you simulate a browser by creating the appropriate FastCgi records that a webserver would create.
	/// </summary>
	class Browser : IDisposable
	{
		private System.Net.IPAddress FastCgiServer;
		private int FastCgiServerPort;
		private Socket SocketToUse;

		public void ExecuteRequest(string url, string method)
		{
			var uri = new Uri(url);
			Socket sock = SocketToUse ?? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			sock.Connect(new IPEndPoint(FastCgiServer, FastCgiServerPort));
			Request req;

			// Begin request
			ushort requestId = 1;
			var beginRequestRecord = new BeginRequestRecord(requestId);
			{
				beginRequestRecord.ApplicationMustCloseConnection = true;
				beginRequestRecord.Role = Role.Responder;
				req = new Request(sock, beginRequestRecord);
				req.Send(beginRequestRecord);
			}

			// Params and empty params
			var firstParams = new ParamsRecord(requestId);
			firstParams.SetParamsFromUri(uri, method);
			req.Send(firstParams);
			var emptyParams = new ParamsRecord(requestId);
			req.Send(emptyParams);

			// Empty Stdin for now.
			//TODO: stdin body for POST
			using (var rec = new StdinRecord(requestId))
			{
				req.Send(rec);
			}

			// Wait for answers and connection closing


			sock.Close();
		}

		public void Dispose ()
		{
			if (SocketToUse != null)
				SocketToUse.Dispose();
		}

		public Browser(Socket notConnectedSocket)
		{
			SocketToUse = notConnectedSocket;
		}

		public Browser(System.Net.IPAddress fcgiServer, int port)
		{
			FastCgiServer = fcgiServer;
			FastCgiServerPort = port;
		}
	}
}
