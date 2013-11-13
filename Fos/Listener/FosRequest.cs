using System;
using FastCgiNet;
using Fos.Owin;
using Fos.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Fos.Listener
{
	enum ConnectionStatus {
		BeginRequestReceived,
		ParamsReceived,
		EmptyStdinReceived,
		EndRequestSent
	}

	internal class FosRequest
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
		
        /// <summary>
        /// The Owin Dictionary. This is null until a ParamsRecord is received.
        /// </summary>
        public OwinContext OwinContext { get; private set; }

		/// <summary>
		/// Sets the application's entry point. This must be set before receiving Stdin records.
		/// </summary>
		public Func<IDictionary<string, object>, Task> ApplicationPipelineEntry;

		private IServerLogger Logger;
		private CancellationTokenSource CancellationSource;

		/// <summary>
		/// This method should (and is) automatically called whenever any part of the body is to be sent. It sends the response's status code
		/// and the response headers.
		/// </summary>
		private void SendHeaders()
		{
			var headers = (IDictionary<string, string[]>)OwinContext["owin.ResponseHeaders"];
			
			//TODO: Can headers length exceed the 65535 content limit of a fastcgi record?
			//WARNING: We may at this point have all the response written.. it would be nice to
			// send more than just the headers here, but everything we have written up to this point in a record
			var headersRecord = new StdoutRecord(RequestId);
			
			using (var writer = new StreamWriter(headersRecord.Contents, System.Text.Encoding.ASCII))
			{
				// Response status code with special CGI header "Status"
				writer.Write("Status: {0}\r\n", OwinContext.ResponseStatusCodeAndReason);

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
			if (OwinContext.ResponseBody.Length == 0)
				throw new InvalidOperationException("No stdin records have been received yet, and as such the response body has not been set up.");

			// Only the last unfilled stream has not been sent yet..
			RecordContentsStream lastStream = OwinContext.ResponseBody.LastUnfilledStream;
			if (lastStream.Length == 0)
				return;

			SendStdoutRecord(lastStream);
		}

		private void SendErrorPage(Exception applicationEx)
		{
			var errorPage = new Fos.CustomPages.ApplicationErrorPage(applicationEx);

			OwinContext.SetResponseHeader("Content-Type", "text/html");
			OwinContext.ResponseStatusCodeAndReason = "500 Internal Server Error";

			using (var sw = new StreamWriter(OwinContext.ResponseBody))
			{
				sw.Write(errorPage.Contents);
			}

			SendUnsentBodyResponse();
		}

		private void SendEmptyResponsePage()
		{
			var emptyResponsePage = new Fos.CustomPages.EmptyResponsePage();
			
			OwinContext.SetResponseHeader("Content-Type", "text/html");
			OwinContext.ResponseStatusCodeAndReason = "500 Internal Server Error";
			
			using (var sw = new StreamWriter(OwinContext.ResponseBody))
			{
				sw.Write(emptyResponsePage.Contents);
			}

			SendUnsentBodyResponse();
		}

		private void SendEndRequest()
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
		private void SendStdoutRecord(RecordContentsStream contents)
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

		private void TestRecord(RecordBase rec)
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
			if (OwinContext == null)
			{
				CancellationSource = new CancellationTokenSource();
				OwinContext = new OwinContext("1.0", CancellationSource.Token);
			}

			OwinContext.AddParamsRecord(rec);
		}

		/// <summary>
		/// Receives an Stdin record, passes it to the Owin Pipeline and returns a Task that when completed, indicates this FastCGI Request has been answered to and is ended.
		/// The task's result indicates if the connection needs to be closed from the application's side.
		/// </summary>
		/// <remarks>This method can return null if there are more Stdin records yet to be received. In fact, the request is only passed to the Owin pipeline after all stdin records have been received.</remarks>
		public Task<bool> ReceiveStdin(StdinRecord rec)
		{
			TestRecord(rec);

			// Append the request body of this record to the entire request body
			OwinContext.RequestBody.AppendStream(rec.Contents);

			// Update status
			if (rec.EmptyContentData)
				Status = ConnectionStatus.EmptyStdinReceived;

			// Only respond if the last empty stdin was received
			if (Status != ConnectionStatus.EmptyStdinReceived)
				return null;

			// Prepare the answer..
			var responseBodyStream = OwinContext.ResponseBody;

			// Sign up for the first write, because we need to send the headers when that happens, and sign up to send
			// general data when records fill up
			responseBodyStream.OnFirstWrite += SendHeaders;
			responseBodyStream.OnStreamFill += SendStdoutRecord;

			// Now pass it to the Owin pipeline
			Task applicationResponse;
			try
			{
				applicationResponse = ApplicationPipelineEntry(OwinContext);
			}
			catch (Exception e)
			{
				if (Logger != null)
					Logger.LogApplicationError(e);

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
						Logger.LogApplicationError(e);

					// Show the exception to the visitor
					SendErrorPage(e);

					// End the FastCgi connection
					SendEndRequest();
					
					// Return a task that indicates completion..
					return ApplicationMustCloseConnection;
				}

				// Send the remaining contents if they haven't been sent (if they are not full)
				RecordContentsStream lastStream = responseBodyStream.LastUnfilledStream;
				if (lastStream.Length > 0)
					SendStdoutRecord(lastStream);
				else if (OwinContext.SomeResponseExists && OwinContext.ResponseBody.Length == 0)
				{
					// If some response exists but the body response stream has not been written to, we must send the headers
					SendHeaders();
				}
				else
				{
					// If we are here, then no response was set by the application, i.e. not a single header or response body
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
		private void SendRecord(RecordBase rec)
		{
			if (rec.RecordType == RecordType.FCGIEndRequest)
				Status = ConnectionStatus.EndRequestSent;

			if (Request.Send(rec) == false)
			{
				CancellationSource.Cancel();
			}
		}

		public FosRequest(Request req, BeginRequestRecord beginRequestRecord, Fos.Logging.IServerLogger logger)
		{
			if (beginRequestRecord == null)
				throw new ArgumentNullException("beginRequestRecord");
			else if (req == null)
				throw new ArgumentNullException("req");

			Logger = logger;
			Status = ConnectionStatus.BeginRequestReceived;
			Request = req;
			ApplicationMustCloseConnection = beginRequestRecord.ApplicationMustCloseConnection;
		}
	}
}
