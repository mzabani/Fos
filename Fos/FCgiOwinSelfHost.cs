using System;
using FastCgiNet;
using FastCgiNet.Logging;
using Fos.Owin;
using Owin;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace Fos
{
	/// <summary>
	/// This is the class you need to use to host your web application. It will create a TCP Socket that receives FastCgi
	/// connections from your web server, passing them to the Owin pipeline.
	/// </summary>
	public class FCgiOwinSelfHost : IDisposable
	{
		FastCgiApplication fastCgiProgram;
		FCgiAppBuilder AppBuilder;
		ConcurrentDictionary<Request, FCgiRequest> requestsStatuses;
		Func<IDictionary<string, object>, System.Threading.Tasks.Task> OwinPipelineEntry;
		Action<IAppBuilder> ApplicationConfigure;
		ILogger logger;
		CancellationTokenSource onAppDisposal;

		/// <summary>
		/// Starts this FastCgi server! This method blocks.
		/// </summary>
		public void Start()
		{
			AppBuilder = new FCgiAppBuilder(onAppDisposal.Token);

			// Configure the application and build our pipeline entry
			ApplicationConfigure(AppBuilder);
			OwinPipelineEntry = (Func<IDictionary<string, object>, System.Threading.Tasks.Task>) AppBuilder.Build(typeof(Func<IDictionary<string, object>, System.Threading.Tasks.Task>));

			fastCgiProgram.OnReceiveBeginRequestRecord += OnReceiveBeginRequest;
			fastCgiProgram.OnReceiveParamsRecord += OnReceiveParams;
			fastCgiProgram.OnReceiveStdinRecord += OnReceiveStdin;

			if (logger != null)
				fastCgiProgram.SetLogger(logger);

			fastCgiProgram.Start();
		}

		public void SetLogger(ILogger logger)
		{
			this.logger = logger;
		}

		void OnReceiveBeginRequest(Request req, Record rec)
		{
			requestsStatuses[req] = new FCgiRequest(req, rec, OwinPipelineEntry, logger);
		}

		void OnReceiveParams(Request req, Record rec) {
			requestsStatuses[req].ReceiveParams(rec);
		}

		void OnReceiveStdin(Request req, Record rec) {
			var onCloseConnection = requestsStatuses[req].ReceiveStdin(rec);
			onCloseConnection.ContinueWith(t =>
			{
				//TODO: The task may have failed or something else happened, verify
				FCgiRequest trash;
				requestsStatuses.TryRemove(req, out trash);
				//TODO: Log failure to remove request from dictionary
				if (t.Result)
					req.CloseSocket();
			});
		}

		/// <summary>
		/// Binds the TCP listen socket to an address an a port.
		/// </summary>
		public void Bind(System.Net.IPAddress addr, int port)
		{
			fastCgiProgram.Bind(addr, port);
		}

		public void Dispose()
		{
			// Tell the application the server is disposing
			onAppDisposal.Cancel();
			onAppDisposal.Dispose();

			fastCgiProgram.Dispose();
		}

		/// <summary>
		/// This constructor lets you specify the method to be executed to register your application's middleware.
		/// </summary>
		public FCgiOwinSelfHost(Action<IAppBuilder> configureMethod)
		{
			ApplicationConfigure = configureMethod;
			requestsStatuses = new ConcurrentDictionary<Request, FCgiRequest>();
			fastCgiProgram = new FastCgiApplication();
			onAppDisposal = new CancellationTokenSource();
		}
	}
}
