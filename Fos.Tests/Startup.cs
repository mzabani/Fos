using System;
using NUnit.Framework;
using Fos;

namespace Fos.Tests
{
	[TestFixture]
	public class Startup
	{
		[Test]
		public void StartAndDispose()
		{
			int port = 9007; // Let's hope this is not being used..

            FosSelfHost server;
			using (server = new FosSelfHost(app => {}))
			{
				server.Bind(System.Net.IPAddress.Loopback, port);

				server.Start(true);
                Assert.AreEqual(true, server.IsRunning);
			}

            Assert.AreEqual(false, server.IsRunning);
		}
	}
}
