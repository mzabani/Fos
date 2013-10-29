using System;
using FastCgiNet;
//using System.Net;
using System.Net.Sockets;

namespace Fos.Tests
{
	/// <summary>
	/// This class helps you simulate a browser by creating the appropriate FastCgi records that a webserver would create.
	/// STILL VERY INCOMPLETE.
	/// </summary>
	class Browser : IDisposable
	{
		private System.Net.IPAddress FastCgiServer;
		private int FastCgiServerPort;

		public void ExecuteRequest(string url)
		{
			using (var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				Request req = new Request(sock);

				// Begin request
				ushort requestId = 1;
				using (var rec = new Record(RecordType.FCGIBeginRequest, requestId))
				{
					req.SetBeginRequest(rec);
					req.Send(rec);
				}

				// Params
				using (var rec = new Record(RecordType.FCGIParams, requestId))
				{
					//TODO: QUERY_STRING, REQUEST_METHOD, SCRIPT_NAME, SERVER_NAME etc.
					rec.NamesAndValues.Content.Add(new NameValuePair("", ""));
				}
			}
		}

		public void Dispose ()
		{
		}

		public Browser (System.Net.IPAddress fcgiServer, int port)
		{
			FastCgiServer = fcgiServer;
			FastCgiServerPort = port;
		}
	}
}
