using System;
using Owin;
using FastCgiNet;
using FastCgiNet.Requests;
using System.Net.Sockets;
using Fos;

namespace Fos.Tests
{
    /// <summary>
    /// Base class for tests with FosSelfHost and other socket related tests.
    /// </summary>
    public class SocketTests
    {
        protected System.Net.IPAddress ListenOn;
        protected int ListenPort;
        
        public SocketTests()
        {
            ListenOn = System.Net.IPAddress.Loopback;
            ListenPort = 9007;
        }
        
        protected Socket ConnectAndGetSocket()
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(ListenOn, ListenPort);
            return sock;
        }

        protected WebServerSocketRequest ConnectAndGetWebServerRequest(ushort requestId)
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(ListenOn, ListenPort);

            return new WebServerSocketRequest(sock, requestId);
        }

        protected FosSelfHost GetApplicationErrorBoundServer()
        {
            Action<IAppBuilder> config = (builder) =>
            {
                builder.Use(typeof(Fos.Tests.Middleware.ThrowsExceptionApplication));
            };
            
            var server = new FosSelfHost(config);
            server.Bind(ListenOn, ListenPort);
            
            return server;
        }

        protected FosSelfHost GetHelloWorldBoundServer()
        {
            Action<IAppBuilder> config = (builder) =>
            {
                builder.Use(typeof(Fos.Tests.Middleware.HelloWorldApplication));
            };
            
            var server = new FosSelfHost(config);
            server.Bind(ListenOn, ListenPort);
            
            return server;
        }
    }
}

