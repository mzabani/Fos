using System;
using FastCgiNet;
using System.Collections.Generic;
using System.Net;
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
		private bool RequestEnded = false;

		private void ReceiveStdout(Request req, StdoutRecord record)
		{
			Console.WriteLine("Received stdout record with length {0}", record.ContentLength);
			ResponseStream.AppendStream(record.Contents);
		}
		private void ReceiveEndRequest(Request req, EndRequestRecord record)
		{
			Console.WriteLine("Received end of request");
			RequestEnded = true;
		}

		public BrowserResponse ExecuteRequest(string url, string method)
		{
			Console.WriteLine("execute request");
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
			//TODO: stdin body for POST
			using (var rec = new StdinRecord(requestId))
			{
				req.Send(rec);
			}

			// Read the answers
			Console.WriteLine("before reader");
			var reader = new SocketReader(sock);
			reader.OnReceiveStdoutRecord += ReceiveStdout;
			reader.OnReceiveEndRequestRecord += ReceiveEndRequest;

			// Wait for things to be done.. busy waiting is bad..
			reader.Start();
			while (!RequestEnded)
				;

			sock.Close();

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
