using System;
using FastCgiNet;
using FastCgiNet.Logging;
using Fos.Owin;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Fos
{
	enum ConnectionStatus {
		BeginRequestReceived,
		ParamsReceived,
		EmptyStdinReceived,
		EndRequestSent
	}

	class FCgiRequest
	{
		public Request Request { get; private set; }
		public ushort RequestId
		{
			get
			{
				return Request.RequestId;
			}
		}
		public bool ApplicationMustCloseConnection { get; private set; }
		public ConnectionStatus Status { get; private set; }
		public OwinContext owinContext { get; private set; }
		public Func<IDictionary<string, object>, Task> PipelineEntry { get; private set; }

		private CancellationTokenSource cancellationSource;
		private ILogger logger;
		
		private void SendErrorPage(Exception applicationEx)
		{
			const string errorPageFormat = "<html><head><title>Application Error</title></head><body><h1>Application Error</h1><p>Your application could not process the request and threw the following exception:</p><p>{0}</p></body></html>";
			string errorPage = string.Format(errorPageFormat, applicationEx);

			owinContext.SetResponseHeader("Content-Type", "text/html");
			if (owinContext.ContainsKey("owin.ResponseStatusCode") == false)
				owinContext.Add("owin.ResponseStatusCode", 500);

			using (var sw = new StreamWriter(owinContext.ResponseBody))
			{
				sw.Write(errorPage);
			}
		}

		void SendEndRequest()
		{
			// End request and connection
			using (var endRequestRec = new Record(RecordType.FCGIEndRequest, RequestId))
			{
				SendRecord(endRequestRec);
			}
		}

		void SendStdoutRecord(Stream contents)
		{
			if (contents == null)
				throw new ArgumentNullException("contents");

			using (var stdoutRec = new Record(RecordType.FCGIStdout, RequestId))
			{
				stdoutRec.ContentStream = contents;
				SendRecord(stdoutRec);
			}
		}

		void TestRecord(Record rec)
		{
			if (rec == null)
				throw new ArgumentNullException("rec");
			else if (rec.RequestId != RequestId)
				throw new ArgumentException("Request id of received record is different than this request's RequestId");
		}

		public void ReceiveParams(Record rec)
		{
			TestRecord(rec);

			Status = ConnectionStatus.ParamsReceived;
			if (owinContext == null)
			{
				cancellationSource = new CancellationTokenSource();
				owinContext = new OwinContext("1.0", cancellationSource.Token);
			}

			owinContext.AddParamsRecord(rec);
		}

		void SendHeaders()
		{
			var headers = (IDictionary<string, string[]>)owinContext["owin.ResponseHeaders"];

			//TODO: Can headers length exceed the 65535 content limit of a fastcgi record?
			//WARNING: We may at this point have all the response written.. it would be nice to
			// send more than just the headers here, but everything we have written up to this point in a record
			Record headersRecord = new Record(RecordType.FCGIStdout, RequestId);

			using (var writer = new StreamWriter(headersRecord.ContentStream, System.Text.Encoding.ASCII))
			{
				foreach (var header in headers)
				{
					writer.Write(header.Key);
					writer.Write(": ");

					for (int i = 0; i < header.Value.Length; ++i)
					{
						string headerValuePart = header.Value[i];

						writer.Write(headerValuePart);

						// If it is not the last, add a comma
						if (i < header.Value.Length - 1)
							writer.Write(", ");
					}
				}

				// Write two newlines
				writer.Write("\r\n\r\n");
			}

			SendRecord(headersRecord);
		}

		/// <summary>
		/// Receives an Stdin record, passes it to the Owin Pipeline and returns a Task that when completed, indicates this FastCGI Request has been answered to and is ended.
		/// The task's result indicates if the connection needs to be closed from the application's side.
		/// </summary>
		/// <remarks>This method can return null if there are more Stdin records yet to be received. In fact, the request is only passed to the Owin pipeline after all stdin records have been received.</remarks>
		public Task<bool> ReceiveStdin(Record rec)
		{
			TestRecord(rec);

			// Update status
			if (rec.EmptyContentData)
				Status = ConnectionStatus.EmptyStdinReceived;

			//TODO: When receiving a lot of stdin records, we really shouldn't write them to the same Stream,
			// because a lot of copying would occur needlessly.
			// We could, instead, wrap all stdin records' Streams in another Stream, and reset the RequestBody stream.
			owinContext.RequestBody = rec.ContentStream;

			// Only respond if the last empty stdin was received
			if (Status != ConnectionStatus.EmptyStdinReceived)
				return null;

			// Prepare the answer..
			// Create a Stream in the Server project that wraps other streams with maximum size. This stream wrapper
			// will then have a method to enumerate these streams, being one stdout record built for each.
			//ALTHOUGH: The StreamWrapper idea is not so good if some middleware changes the underlying stream.. It is not clear 
			// if this is valid in the specs, but it seems safe to assume it is not valid.
			var responseStream = new FragmentedResponseStream<RecordContentsStream>();

			// Sign up for the first write, because we need to send the headers when that happens, and sign up to send
			// general data when records fill up
			responseStream.OnFirstWrite += SendHeaders;
			responseStream.OnStreamFill += SendStdoutRecord;
			owinContext.ResponseBody = responseStream;

			// Now pass it to the Owin pipeline
			Task applicationResponse;
			try
			{
				applicationResponse = PipelineEntry(owinContext);
			}
			catch (Exception e)
			{
				if (logger != null)
					logger.Error(e, "Owin Application error");

				// Show the exception to the visitor
				SendErrorPage(e);

				// End the FastCgi connection
				SendEndRequest();

				// Return a task that indicates completion..
				return Task.Factory.StartNew<bool>(() => ApplicationMustCloseConnection);
			}

			// Now set the actions to do when the response is ready
			return Task.Factory.StartNew<bool>(() =>
            {
				try
				{
					applicationResponse.Wait();
				}
				catch (Exception e)
				{
					if (logger != null)
						logger.Error(e, "Owin Application error");

					// Show the exception to the visitor
					SendErrorPage(e);

					// End the FastCgi connection
					SendEndRequest();
					
					// Return a task that indicates completion..
					return true;
				}

				// Send the remaining contents if they haven't been sent (if they are not full)
				RecordContentsStream lastStream = responseStream.LastUnfilledStream;
				if (lastStream.Length > 0)
					SendStdoutRecord(lastStream);

				// Send empty stdout record
				SendStdoutRecord(Stream.Null);

				SendEndRequest();

				return ApplicationMustCloseConnection;
			});
		}

		/// <summary>
		/// Use this method to send records internally, because it maintains state.
		/// </summary>
		/// <param name="rec">Rec.</param>
		void SendRecord(Record rec)
		{
			if (rec.RecordType == RecordType.FCGIEndRequest)
				Status = ConnectionStatus.EndRequestSent;

			if (Request.Send(rec) == false)
			{
				cancellationSource.Cancel();
			}
		}

		public FCgiRequest (Request req, Record beginRequestRecord, Func<IDictionary<string, object>, Task> pipelineEntry, ILogger logger)
		{
			if (beginRequestRecord == null)
				throw new ArgumentNullException("beginRequestRecord");
			else if (beginRequestRecord.RecordType != RecordType.FCGIBeginRequest)
				throw new ArgumentException("Record must be of BeginRequest type");

			if (req == null)
				throw new ArgumentNullException("req");
			if (pipelineEntry == null)
				throw new ArgumentNullException("pipelineEntry");

			this.logger = logger;
			Status = ConnectionStatus.BeginRequestReceived;
			Request = req;
			PipelineEntry = pipelineEntry;
			ApplicationMustCloseConnection = beginRequestRecord.BeginRequest.ApplicationMustCloseConnection;
		}
	}
}
