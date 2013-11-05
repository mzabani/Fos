using System;
using NUnit.Framework;
using Fos;

namespace Fos.Tests
{
	[TestFixture]
	public class Startup
	{
		public void StartAndDispose()
		{
			int port = 9007; // Let's hope this is not being used..

			using (var server = new FosSelfHost(app => {}))
			{
				server.Bind(System.Net.IPAddress.Loopback, port);

				server.Start(true);
			}
		}
	}
}
