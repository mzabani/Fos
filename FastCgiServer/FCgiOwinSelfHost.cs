using System;
using FastCgiNet;
using Owin;
using System.Collections.Generic;

namespace FastCgiServer
{
	/// <summary>
	/// This is the class you need to use to host your web application. It will create a TCP Socket that receives FastCgi
	/// connections from your web server, passing them to the Owin pipeline.
	/// </summary>
	public class FCgiOwinSelfHost : IDisposable
	{
		FastCgiApplication fastCgiProgram;
		Dictionary<Request, FCgiRequest> requestsStatuses;
		FCgiAppBuilder AppBuilder;
		Func<IDictionary<string, object>, System.Threading.Tasks.Task> OwinPipelineEntry;
		Action<IAppBuilder> ApplicationConfigure;

		/// <summary>
		/// Starts this FastCgi server! This method blocks.
		/// </summary>
		public void Start()
		{
			AppBuilder = new FCgiAppBuilder();

			// Configure the application and build our pipeline entry
			ApplicationConfigure(AppBuilder);
			OwinPipelineEntry = (Func<IDictionary<string, object>, System.Threading.Tasks.Task>) AppBuilder.Build(typeof(Func<IDictionary<string, object>, System.Threading.Tasks.Task>));

			fastCgiProgram.OnReceiveBeginRequestRecord += OnReceiveBeginRequest;
			fastCgiProgram.OnReceiveParamsRecord += OnReceiveParams;
			fastCgiProgram.OnReceiveStdinRecord += OnReceiveStdin;
			fastCgiProgram.Start();
		}

		void OnReceiveBeginRequest(Request req, Record rec) {
			//TODO: Maybe we didn't have time to remove a different connection with the same requestid from the dictionary yet.. ?
			requestsStatuses.Add(req, new FCgiRequest(req, rec, OwinPipelineEntry));
		}

		void OnReceiveParams(Request req, Record rec) {
			requestsStatuses[req].ReceiveParams(rec);
		}

		void OnReceiveStdin(Request req, Record rec) {
			var onCloseConnection = requestsStatuses[req].ReceiveStdin(rec);
			onCloseConnection.ContinueWith(t => {
				//TODO: The task may have failed or something else happened, verify
				requestsStatuses.Remove(req);
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
			fastCgiProgram.Dispose ();
		}

		/// <summary>
		/// This constructor lets you specify the method to be executed to register your application's middleware.
		/// </summary>
		public FCgiOwinSelfHost(Action<IAppBuilder> configureMethod)
		{
			ApplicationConfigure = configureMethod;
			requestsStatuses = new Dictionary<Request, FCgiRequest>();
			fastCgiProgram = new FastCgiApplication();
		}
	}
}
