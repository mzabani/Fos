using System;
using NUnit.Framework;
using Fos;

namespace Fos.Tests
{
	[TestFixture]
	public class Startup
	{
        [Test]
        public void CloseAndFlush()
        {
            var memStream = new System.IO.MemoryStream();

            memStream.Close();
            memStream.Flush();
        }

		[Test]
		public void StartAndDisposeTcpSocket()
		{
			int port = 9007; // Let's hope this is not being used..

            FosSelfHost server;
			using (server = new FosSelfHost(app => {}))
			{
				server.Bind(System.Net.IPAddress.Loopback, port);

				server.Start(true);
                Assert.AreEqual(true, server.IsRunning);
			}

            using (server = new FosSelfHost(app => {}))
            {
                server.Bind(System.Net.IPAddress.Loopback, port);
                
                server.Start(true);
                Assert.AreEqual(true, server.IsRunning);
            }

            Assert.AreEqual(false, server.IsRunning);
		}

#if __MonoCS__
        [Test]
        public void StartAndDisposeUnixSocket()
        {
            FosSelfHost server;
            using (server = new FosSelfHost(app => {}))
            {
                server.Bind("./.FosTestSocket");
                
                server.Start(true);
                Assert.AreEqual(true, server.IsRunning);
            }

            using (server = new FosSelfHost(app => {}))
            {
                server.Bind("./.FosTestSocket");
                
                server.Start(true);
                Assert.AreEqual(true, server.IsRunning);
            }
            
            Assert.AreEqual(false, server.IsRunning);
        }
#endif
	}
}
