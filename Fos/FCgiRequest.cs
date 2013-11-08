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

		private FragmentedRequestStream<RecordContentsStream> RequestBodyStream;
		private FragmentedResponseStream<RecordContentsStream> ResponseBodyStream;
		private CancellationTokenSource CancellationSource;
		private ILogger Logger;

		/// <summary>
		/// This method should (and is) automatically called whenever any part of the body is to be sent. It sends the response's status code
		/// and the response headers.
		/// </summary>
		void SendHeaders()
		{
			var headers = (IDictionary<string, string[]>)owinContext["owin.ResponseHeaders"];
			
			//TODO: Can headers length exceed the 65535 content limit of a fastcgi record?
			//WARNING: We may at this point have all the response written.. it would be nice to
			// send more than just the headers here, but everything we have written up to this point in a record
			var headersRecord = new StdoutRecord(RequestId);
			
			using (var writer = new StreamWriter(headersRecord.Contents, System.Text.Encoding.ASCII))
			{
				// Response status code with special CGI header "Status"
				writer.Write("Status: {0}\r\n", owinContext.ResponseStatusCode);

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
					
					writer.Write("\r\n");
				}
				
				// There should be a newline after the last header. Add a second one before the body
				writer.Write("\r\n");
			}
			
			SendRecord(headersRecord);
		}

		/// <summary>
		/// Sends all parts of the response's body (no headers) that haven't been sent. If there is nothing to be sent,
		/// this method does nothing (i.e. it does not send an empty FastCgi record).
		/// </summary>
		private void SendUnsentBodyResponse()
		{
			if (ResponseBodyStream == null)
				throw new InvalidOperationException("No stdin records have been received yet, and as such the response body has not been set up.");

			// Only the last unfilled stream has not been sent yet..
			RecordContentsStream lastStream = ResponseBodyStream.LastUnfilledStream;
			if (lastStream.Length == 0)
				return;

			SendStdoutRecord(lastStream);
		}

		private void SendErrorPage(Exception applicationEx)
		{
			var errorPage = new Fos.CustomPages.ApplicationErrorPage(applicationEx);

			owinContext.SetResponseHeader("Content-Type", "text/html");
			owinContext.ResponseStatusCode = "500 Internal Server Error";

			using (var sw = new StreamWriter(owinContext.ResponseBody))
			{
				sw.Write(errorPage.Contents);
			}

			SendUnsentBodyResponse();
		}

		private void SendEmptyResponsePage()
		{
			var emptyResponsePage = new Fos.CustomPages.EmptyResponsePage();
			
			owinContext.SetResponseHeader("Content-Type", "text/html");
			owinContext.ResponseStatusCode = "500 Internal Server Error";
			
			using (var sw = new StreamWriter(owinContext.ResponseBody))
			{
				sw.Write(emptyResponsePage.Contents);
			}

			SendUnsentBodyResponse();
		}

		void SendEndRequest()
		{
			// End request and connection
			var endRequestRec = new EndRequestRecord(RequestId);
			SendRecord(endRequestRec);
		}

		/// <summary>
		/// Sends a Stdout Record with its contents set to <paramref name="contents"/>. The stream passed is disposed
		/// after the record is sent.
		/// </summary>
		/// <param name="contents">A stream with the record's contents. If null, an empty record is sent.</param> 
		void SendStdoutRecord(RecordContentsStream contents)
		{
			if (contents == null)
			{
				using (var stdoutRec = new StdoutRecord(RequestId))
				{
					SendRecord(stdoutRec);
				}
			}
			else
			{
				using (var stdoutRec = new StdoutRecord(RequestId))
				{
					contents.Seek(0, SeekOrigin.Begin);
					stdoutRec.Contents = contents;
					SendRecord(stdoutRec);
				}
			}
		}

		void TestRecord(RecordBase rec)
		{
			if (rec == null)
				throw new ArgumentNullException("rec");
			else if (rec.RequestId != RequestId)
				throw new ArgumentException("Request id of received record is different than this request's RequestId");
		}

		public void ReceiveParams(ParamsRecord rec)
		{
			TestRecord(rec);

			Status = ConnectionStatus.ParamsReceived;
			if (owinContext == null)
			{
				CancellationSource = new CancellationTokenSource();
				owinContext = new OwinContext("1.0", CancellationSource.Token);
			}

			owinContext.AddParamsRecord(rec);
		}

		/// <summary>
		/// Receives an Stdin record, passes it to the Owin Pipeline and returns a Task that when completed, indicates this FastCGI Request has been answered to and is ended.
		/// The task's result indicates if the connection needs to be closed from the application's side.
		/// </summary>
		/// <remarks>This method can return null if there are more Stdin records yet to be received. In fact, the request is only passed to the Owin pipeline after all stdin records have been received.</remarks>
		public Task<bool> ReceiveStdin(StdinRecord rec)
		{
			TestRecord(rec);

			// If this is the first stdin record, prepare the request body
			if (RequestBodyStream == null)
			{
				RequestBodyStream = new FragmentedRequestStream<RecordContentsStream>();
				owinContext.RequestBody = RequestBodyStream;
			}

			RequestBodyStream.AppendStream(rec.Contents);

			/*
			using (var reader = new StreamReader(rec.Contents))
			{
				Console.WriteLine(reader.ReadToEnd());
			}
			rec.Contents.Seek(0, SeekOrigin.Begin);
			*/

			// Update status
			if (rec.EmptyContentData)
				Status = ConnectionStatus.EmptyStdinReceived;

			// Only respond if the last empty stdin was received
			if (Status != ConnectionStatus.EmptyStdinReceived)
				return null;

			// Prepare the answer..
			// Create a Stream in the Server project that wraps other streams with maximum size. This stream wrapper
			// will then have a method to enumerate these streams, being one stdout record built for each.
			//ALTHOUGH: The StreamWrapper idea is not so good if some middleware changes the underlying stream.. It is not clear 
			// if this is valid in the specs, but it seems safe to assume it is not valid.
			ResponseBodyStream = new FragmentedResponseStream<RecordContentsStream>();

			// Sign up for the first write, because we need to send the headers when that happens, and sign up to send
			// general data when records fill up
			ResponseBodyStream.OnFirstWrite += SendHeaders;
			ResponseBodyStream.OnStreamFill += SendStdoutRecord;
			owinContext.ResponseBody = ResponseBodyStream;

			// Now pass it to the Owin pipeline
			Task applicationResponse;
			try
			{
				applicationResponse = PipelineEntry(owinContext);
			}
			catch (Exception e)
			{
				if (Logger != null)
					Logger.Error(e, "Owin Application error");

				// Show the exception to the visitor
				SendErrorPage(e);

				// End the FastCgi connection
				SendEndRequest();

				// Return a task that indicates completion..
				return Task.Factory.StartNew<bool>(() => ApplicationMustCloseConnection);
			}

			// Now set the actions to do when the response is ready
			return applicationResponse.ContinueWith<bool>(applicationTask =>
            {
				if (applicationTask.IsFaulted)
				{
					Exception e = applicationTask.Exception;

					if (Logger != null)
						Logger.Error(e, "Owin Application error");

					// Show the exception to the visitor
					SendErrorPage(e);

					// End the FastCgi connection
					SendEndRequest();
					
					// Return a task that indicates completion..
					return ApplicationMustCloseConnection;
				}

				// Send the remaining contents if they haven't been sent (if they are not full)
				RecordContentsStream lastStream = ResponseBodyStream.LastUnfilledStream;
				if (lastStream.Length > 0)
					SendStdoutRecord(lastStream);
				else
				{
					// The last unfilled stream is of size zero if and only if nothing was written to the output.
					// This could be intended behavior from the application, unless of course the headers
					// are not set either. If that is the case, then show an empty response error
					SendEmptyResponsePage();
				}

				// Send empty stdout record
				SendStdoutRecord(null);

				SendEndRequest();

				return ApplicationMustCloseConnection;
			});
		}

		/// <summary>
		/// Use this method to send records internally, because it maintains state.
		/// </summary>
		/// <param name="rec">Rec.</param>
		void SendRecord(RecordBase rec)
		{
			if (rec.RecordType == RecordType.FCGIEndRequest)
				Status = ConnectionStatus.EndRequestSent;

			if (Request.Send(rec) == false)
			{
				CancellationSource.Cancel();
			}
		}

		public FCgiRequest (Request req, BeginRequestRecord beginRequestRecord, Func<IDictionary<string, object>, Task> pipelineEntry, ILogger logger)
		{
			if (beginRequestRecord == null)
				throw new ArgumentNullException("beginRequestRecord");
			else if (req == null)
				throw new ArgumentNullException("req");
			else if (pipelineEntry == null)
				throw new ArgumentNullException("pipelineEntry");

			this.Logger = logger;
			Status = ConnectionStatus.BeginRequestReceived;
			Request = req;
			PipelineEntry = pipelineEntry;
			ApplicationMustCloseConnection = beginRequestRecord.ApplicationMustCloseConnection;
		}
	}
}
