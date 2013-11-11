using System;
using FastCgiNet;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.IO;

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

		private FragmentedRequestStream<RecordContentsStream> ResponseStream = new FragmentedRequestStream<RecordContentsStream>();

		public BrowserResponse ExecuteRequest(string url, string method)
		{
			var uri = new Uri(url);
			Socket sock = SocketToUse ?? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			sock.Connect(new IPEndPoint(FastCgiServer, FastCgiServerPort));

			var reader = new ByteReader(new RecordFactory());

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
			using (var firstParams = new ParamsRecord(requestId))
			{
				firstParams.SetParamsFromUri(uri, method);
				req.Send(firstParams);
			}
			using (var emptyParams = new ParamsRecord(requestId))
			{
				req.Send(emptyParams);
			}

			// Empty Stdin for now.
			using (var rec = new StdinRecord(requestId))
			{
				req.Send(rec);
			}

			// Build our records!
			byte[] buf = new byte[4096];
			int bytesRead;
			ResponseStream = new FragmentedRequestStream<RecordContentsStream>();
			while ((bytesRead = sock.Receive(buf)) > 0)
			{
				foreach (RecordBase rec in reader.Read(buf, 0, bytesRead))
				{
					if (rec.RecordType == RecordType.FCGIStdout)
					{
						var contents = ((StreamRecord)rec).Contents;
						ResponseStream.AppendStream(contents);
					}
				}
			}

			sock.Close();

			Console.WriteLine ("Response stream has {0} underlying streams, of summed length {1}", ResponseStream.UnderlyingStreams.Count(), ResponseStream.UnderlyingStreams.Sum(x => x.Length));
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
