using System;
using FastCgiNet;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.IO;
using FastCgiNet.Streams;

namespace Fos.Tests
{
	/// <summary>
	/// This class helps you simulate a browser by creating the appropriate FastCgi records that a webserver would create.
	/// </summary>
	class Browser
	{
		private System.Net.IPAddress FastCgiServer;
		private int FastCgiServerPort;
		private Socket SocketToUse;

        protected FastCgiStream ResponseStream;
        protected SocketRequest Request;
        protected ushort RequestId
        {
            get
            {
                return Request.RequestId;
            }
        }

        protected void SendBeginRequest(Socket sock)
        {
            ushort requestId = 1;
            var beginRequestRecord = new BeginRequestRecord(requestId);
            beginRequestRecord.ApplicationMustCloseConnection = true;
            beginRequestRecord.Role = Role.Responder;
            Request = new SocketRequest(sock, beginRequestRecord, true);
            Request.Send(beginRequestRecord);
        }

        protected void SendParams(Uri uri, string method)
        {
            using (var firstParams = new ParamsRecord(RequestId))
            {
                firstParams.SetParamsFromUri(uri, method);
                Request.Send(firstParams);
            }
            using (var emptyParams = new ParamsRecord(RequestId))
            {
                Request.Send(emptyParams);
            }
        }

		public virtual BrowserResponse ExecuteRequest(string url, string method)
		{
			var uri = new Uri(url);
			Socket sock = SocketToUse ?? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            ResponseStream = new FastCgiNet.Streams.SocketStream(sock, RecordType.FCGIStdout, true);

			sock.Connect(new IPEndPoint(FastCgiServer, FastCgiServerPort));

			var reader = new RecordFactory();

            // Begin request
            SendBeginRequest(sock);

			// Params and empty params
            SendParams(uri, method);

			// Empty Stdin for now.
			using (var rec = new StdinRecord(RequestId))
			{
				Request.Send(rec);
			}

			// Build our records!
			byte[] buf = new byte[4096];
			int bytesRead;
			while ((bytesRead = sock.Receive(buf)) > 0)
			{
				foreach (RecordBase rec in reader.Read(buf, 0, bytesRead))
				{
					if (rec.RecordType == RecordType.FCGIStdout)
					{
						var contents = ((StreamRecordBase)rec).Contents;
						ResponseStream.AppendStream(contents);
					}
				}
			}

            sock.Dispose();

			return new BrowserResponse(ResponseStream);
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
