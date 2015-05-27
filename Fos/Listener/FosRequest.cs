using System;
using FastCgiNet;
using Fos.Owin;
using Fos.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using FastCgiNet.Streams;
using System.Net.Sockets;

namespace Fos.Listener
{
    internal delegate void SocketClosed();

	internal class FosRequest : FastCgiNet.Requests.ApplicationSocketRequest
	{
        private Fos.Streams.FosStdoutStream stdout;
        public override FastCgiStream Stdout
        {
            get
            {
                return stdout;
            }
        }

        /// <summary>
        /// Some applications need to control when flushing the response to the visitor is done; if that is the case, set this to <c>false</c>.
        /// If the application does not require that, then Fos will flush the response itself periodically to avoid keeping too 
        /// many buffers around and to avoid an idle connection.
        /// </summary>
        public bool FlushPeriodically;

        /// <summary>
        /// The Owin Context dictionary. This is never null.
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
			
			using (var headerStream = new Fos.Streams.NonEndingStdoutSocketStream(Socket))
            {
                headerStream.RequestId = RequestId;

                using (var writer = new Fos.Streams.HeaderWriter(headerStream))
                {
                    // Response status code with special CGI header "Status"
                    writer.Write("Status", OwinContext.ResponseStatusCodeAndReason);

                    foreach (var header in headers)
                    {
                        writer.Write(header.Key, header.Value);
                    }

                    // That last newline
                    writer.WriteLine();
                }
            }
		}

		private void SendErrorPage(Exception applicationEx)
		{
			var errorPage = new Fos.CustomPages.ApplicationErrorPage(applicationEx);

			OwinContext.SetResponseHeader("Content-Type", "text/html");
			OwinContext.ResponseStatusCodeAndReason = "500 Internal Server Error";

			using (var sw = new StreamWriter(Stdout))
			{
				sw.Write(errorPage.Contents);
			}

            Stdout.Flush();
		}

		private void SendEmptyResponsePage()
		{
			var emptyResponsePage = new Fos.CustomPages.EmptyResponsePage();
			
			OwinContext.SetResponseHeader("Content-Type", "text/html");
			OwinContext.ResponseStatusCodeAndReason = "500 Internal Server Error";
			
			using (var sw = new StreamWriter(Stdout))
			{
				sw.Write(emptyResponsePage.Contents);
			}

            Stdout.Flush();
		}

        protected override void AddReceivedRecord(RecordBase rec)
        {
            base.AddReceivedRecord(rec);

            switch (rec.RecordType)
            {
                case RecordType.FCGIParams:
                    if (Params.IsComplete)
                        OwinContext.AddParams(Params);
                    break;

                case RecordType.FCGIStdin:
                    // Only respond if the last empty stdin was received
                    if (!Stdin.IsComplete)
                        break;

                    var onApplicationDone = ProcessRequest();
                    onApplicationDone.ContinueWith(t => {
                        // This task _CANNOT_ fail
                        if (ApplicationMustCloseConnection)
                        {
                            this.Dispose();
                        }
                    });
                    break;
            }
        }

		/// <summary>
		/// Receives an Stdin record, passes it to the Owin Pipeline and returns a Task that when completed, indicates this FastCGI Request has been answered to and is ended.
		/// The task's result indicates if the connection needs to be closed from the application's side.
		/// </summary>
		/// <remarks>This method can return null if there are more Stdin records yet to be received. In fact, the request is only passed to the Owin pipeline after all stdin records have been received.</remarks>
		public Task ProcessRequest()
		{
			// Sign up for the first write, because we need to send the headers when that happens (Owin spec)
            // Also sign up to flush buffers for the application if we can
			stdout.OnFirstWrite += SendHeaders;
            if (FlushPeriodically)
                stdout.OnStreamFill += () => stdout.Flush();

			// Now pass it to the Owin pipeline
			Task applicationResponse;
			try
			{
				applicationResponse = ApplicationPipelineEntry(OwinContext);
			}
			catch (Exception e)
			{
				if (Logger != null)
					Logger.LogApplicationError(e, new RequestInfo(this));

				// Show the exception to the visitor
				SendErrorPage(e);

				// End the FastCgi connection with an error code
                SendEndRequest(-1, ProtocolStatus.RequestComplete);

				// Return a task that indicates completion..
                return Task.Factory.StartNew(() => {});
			}

			// Now set the actions to do when the response is ready
			return applicationResponse.ContinueWith(applicationTask =>
            {
				if (applicationTask.IsFaulted)
				{
					Exception e = applicationTask.Exception;

					if (Logger != null)
						Logger.LogApplicationError(e, new RequestInfo(this));

					// Show the exception to the visitor
					SendErrorPage(e);

                    SendEndRequest(-1, ProtocolStatus.RequestComplete);
				}
				else if (!OwinContext.SomeResponseExists)
                {
					// If we are here, then no response was set by the application, i.e. not a single header or response body
					SendEmptyResponsePage();

                    SendEndRequest(-1, ProtocolStatus.RequestComplete);
				}
                else
                {
                    // If no data has been written (e.g. 304 w/- empty response), the headers won't have been written
                    if (stdout.Length == 0)
                    {
                        SendHeaders();
                    }

                    // Signal successful return status
                    SendEndRequest(0, ProtocolStatus.RequestComplete);
                }
			});
		}

		protected override void Send(RecordBase rec)
		{
            try
            {
                base.Send(rec);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException e)
            {
                if (!SocketHelper.IsConnectionAbortedByTheOtherSide(e))
                    throw;
                
                CancellationSource.Cancel();
            }
		}

        /// <summary>
        /// Warns when the socket for this request was closed by our side because <see cref="CloseSocket()"/> was called.
        /// </summary>
        public event SocketClosed OnSocketClose = delegate {};

        protected override void CloseSocket()
        {
            try
            {
                if (!CancellationSource.IsCancellationRequested)
                    CancellationSource.Cancel();

                base.CloseSocket();
                OnSocketClose();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException e)
            {
                if (!SocketHelper.IsConnectionAbortedByTheOtherSide(e))
                    throw;
            }
        }

		public FosRequest(Socket sock, Fos.Logging.IServerLogger logger)
            : base(sock)
		{
			Logger = logger;
            CancellationSource = new CancellationTokenSource();
            OwinContext = new OwinContext("1.0", CancellationSource.Token);

            // Streams
            stdout = new Fos.Streams.FosStdoutStream(sock);
            OwinContext.ResponseBody = Stdout;
            OwinContext.RequestBody = Stdin;
		}
	}
}
