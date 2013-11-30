using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.IO;
using FastCgiNet;
using FastCgiNet.Streams;
using FastCgiNet.Requests;

namespace Fos.Tests
{
	/// <summary>
	/// This class helps you simulate a browser by creating the appropriate FastCgi records that a webserver would create.
	/// </summary>
	class Browser : IDisposable
	{
		private System.Net.IPAddress FastCgiServer;
		private int FastCgiServerPort;
        private int MaxPollTime = 10000000;

        protected FastCgiStream ResponseStream;
        protected WebServerSocketRequest Request;
        protected ushort RequestId
        {
            get
            {
                return Request.RequestId;
            }
        }

        protected void SendParams(Uri uri, string method)
        {
            using (var nvpWriter = new NvpWriter(Request.Params))
            {
                nvpWriter.WriteParamsFromUri(uri, method);
            }
        }

		public virtual BrowserResponse ExecuteRequest(string url, string method)
		{
			Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(new IPEndPoint(FastCgiServer, FastCgiServerPort));

            // Our request
            ushort requestId = 1;
            Request = new WebServerSocketRequest(sock, requestId);
            Request.SendBeginRequest(Role.Responder, true);
            SendParams(new Uri(url), method);
            Request.SendEmptyStdin();

			// Receive the data from the other side
            if (!sock.Poll(MaxPollTime, SelectMode.SelectRead))
                throw new Exception("Data took too long");

			byte[] buf = new byte[4096];
			int bytesRead;
            bool endRequest = false;
			while ((bytesRead = sock.Receive(buf)) > 0)
            {
                endRequest = Request.FeedBytes(buf, 0, bytesRead).Any(x => x.RecordType == RecordType.FCGIEndRequest);
            }
            if (!endRequest)
                throw new Exception("EndRequest was not received.");

			return new BrowserResponse(Request.Stdout);
		}

        public void Dispose()
        {
            if (Request != null)
                Request.Dispose();

            if (ResponseStream != null)
                ResponseStream.Dispose();
        }

		public Browser(System.Net.IPAddress fcgiServer, int port)
		{
			FastCgiServer = fcgiServer;
			FastCgiServerPort = port;
		}
	}
}
