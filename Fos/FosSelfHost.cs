using System;
using System.Collections.Generic;
using System.Threading;
using FastCgiNet;
using Fos.Owin;
using Fos.Listener;
using Fos.Logging;
using Owin;

namespace Fos
{
	/// <summary>
	/// This is the class you need to use to host your web application. It will create a TCP Socket that receives FastCgi
	/// connections from your web server, passing them to the Owin pipeline.
	/// </summary>
	public class FosSelfHost : IDisposable
	{
		private SocketListener FastCgiListener;
		private FCgiAppBuilder AppBuilder;
		private Func<IDictionary<string, object>, System.Threading.Tasks.Task> OwinPipelineEntry;
		private Action<IAppBuilder> ApplicationConfigure;
		private IServerLogger Logger;
		private CancellationTokenSource OnAppDisposal;

        public bool IsRunning
        {
            get
            {
                return FastCgiListener.IsRunning;
            }
        }

		/// <summary>
		/// Starts this FastCgi server! This method only returns when the server is ready to accept connections.
		/// </summary>
		/// <param name="background">True to start the server without blocking, false to block.</param>
		public void Start(bool background)
		{
			AppBuilder = new FCgiAppBuilder(OnAppDisposal.Token);

			// Configure the application and build our pipeline entry
			ApplicationConfigure(AppBuilder);
			OwinPipelineEntry = (Func<IDictionary<string, object>, System.Threading.Tasks.Task>) AppBuilder.Build(typeof(Func<IDictionary<string, object>, System.Threading.Tasks.Task>));

			FastCgiListener.OnReceiveBeginRequestRecord += OnReceiveBeginRequest;
			FastCgiListener.OnReceiveParamsRecord += OnReceiveParams;
			FastCgiListener.OnReceiveStdinRecord += OnReceiveStdin;

			if (Logger != null)
				FastCgiListener.SetLogger(Logger);

            FastCgiListener.Start(background);
		}

		/// <summary>
		/// Notifies the application (in non-standard fashion) that the server is stopping and stops listening for connections, while
		/// closing active connections abruptly.
		/// </summary>
		public void Stop()
		{
			// Tell the application the server is disposing
			OnAppDisposal.Cancel();
			OnAppDisposal.Dispose();

			FastCgiListener.Stop();
		}

		public void SetLogger(IServerLogger logger)
		{
            if (logger == null)
                throw new ArgumentNullException("logger");

			this.Logger = logger;
		}

		private void OnReceiveBeginRequest(FosRequest req, BeginRequestRecord rec) {
			req.ApplicationPipelineEntry = OwinPipelineEntry;
		}

		private void OnReceiveParams(FosRequest req, ParamsRecord rec) {
			req.ReceiveParams(rec);
		}

		private void OnReceiveStdin(FosRequest req, StdinRecord rec) {
			var onCloseConnection = req.ReceiveStdin(rec);
			if (onCloseConnection == null)
				return;

			onCloseConnection.ContinueWith(t =>
			{
				//TODO: The task may have failed or something else happened, verify
                // Remember that connections closed by the other side abruptly have already
                // been closed by the listener loop, so we shouldn't call Request.CloseSocket() here again
				if (t.Result)
					req.Request.CloseSocket();
			});
		}

		/// <summary>
		/// Binds the TCP listen socket to an address an a port.
		/// </summary>
		public void Bind(System.Net.IPAddress addr, int port)
		{
			FastCgiListener.Bind(addr, port);
		}

#if __MonoCS__
		/// <summary>
		/// Defines the unix socket path to listen on.
		/// </summary>
		public void Bind(string socketPath)
		{
			FastCgiListener.Bind(socketPath);
		}
#endif

		public void Dispose()
		{
			Stop();
			OnAppDisposal.Dispose();
			FastCgiListener.Dispose();
		}

		/// <summary>
		/// This constructor lets you specify the method to be executed to register your application's middleware.
		/// </summary>
		public FosSelfHost(Action<IAppBuilder> configureMethod)
		{
			ApplicationConfigure = configureMethod;
			FastCgiListener = new SocketListener();
			OnAppDisposal = new CancellationTokenSource();
		}
	}
}
