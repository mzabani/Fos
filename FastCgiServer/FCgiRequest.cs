using System;
using FastCgiNet;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace FastCgiServer
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
		public OwinParameters owinParameters { get; private set; }
		public Func<IDictionary<string, object>, Task> PipelineEntry { get; private set; }

		LinkedList<Record> ReceivedStdinRecords;

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
			//TODO: Something goes very wrong if contents is Stream.Null
			if (contents != null)
			{
				using (var stdoutRec = new Record(RecordType.FCGIStdout, RequestId))
				{
					stdoutRec.ContentStream = contents;
					SendRecord(stdoutRec);
				}
			}
			else
			{
				using (var stdoutRec = new Record(RecordType.FCGIStdout, RequestId))
				{
					SendRecord(stdoutRec);
				}
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
			if (owinParameters == null)
				owinParameters = new OwinParameters();
			owinParameters.AddParamsRecord(rec);
		}

		void SendHeaders()
		{
			var headers = (IDictionary<string, string[]>)owinParameters["owin.ResponseHeaders"];

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
			owinParameters.RequestBody = rec.ContentStream;

			// Only respond if the last empty stdin was received
			if (Status != ConnectionStatus.EmptyStdinReceived)
				return null;

			// Prepare the answer..
			// Create a Stream in the Server project that wraps other streams with maximum size. This stream wrapper
			// will then have a method to enumerate these streams, being one stdout record built for each.
			//ALTHOUGH: The StreamWrapper idea is not so good if some middleware changes the underlying stream.. It is not clear 
			// if this is valid in the specs, but it seems safe to assume it is not valid.
			var responseStream = new OwinResponseStream<RecordContentsStream>();

			// Sign up for the first write, because we need to send the headers when that happens, and sign up to send
			// general data when records fill up
			responseStream.OnFirstWrite += SendHeaders;
			responseStream.OnStreamFill += SendStdoutRecord;
			owinParameters.ResponseBody = responseStream;

			// Now pass it to the Owin pipeline
			Task applicationResponse;
			try
			{
				applicationResponse = PipelineEntry(owinParameters);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				//TODO: Log it and send stderr records with the exception and an endrequest

				SendEndRequest();

				// Return a task that indicates completion..
				return Task.Factory.StartNew<bool>(() => true);
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
					Console.WriteLine(e);
					//TODO: Log it and send stderr records with the exception and an endrequest
					
					SendEndRequest();
					
					// Return a task that indicates completion..
					return true;
				}

				// Send the remaining contents if they haven't been sent (if they are not full)
				RecordContentsStream lastStream = responseStream.LastUnfilledStream;
				if (lastStream.Length > 0)
					SendStdoutRecord(lastStream);

				// Send empty stdout record
				//SendStdoutRecord(Stream.Null);
				SendStdoutRecord(null);

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

			Request.Send(rec);
		}

		public FCgiRequest (Request req, Record beginRequestRecord, Func<IDictionary<string, object>, Task> pipelineEntry)
		{
			if (beginRequestRecord == null)
				throw new ArgumentNullException("beginRequestRecord");
			else if (beginRequestRecord.RecordType != RecordType.FCGIBeginRequest)
				throw new ArgumentException("Record must be of BeginRequest type");

			if (req == null)
				throw new ArgumentNullException("req");
			if (pipelineEntry == null)
				throw new ArgumentNullException("pipelineEntry");

			Status = ConnectionStatus.BeginRequestReceived;
			Request = req;
			PipelineEntry = pipelineEntry;
			ApplicationMustCloseConnection = beginRequestRecord.BeginRequest.ApplicationMustCloseConnection;
			ReceivedStdinRecords = new LinkedList<Record>();
		}
	}
}
