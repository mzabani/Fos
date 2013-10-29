using System;
using NUnit.Framework;
using Fos;
using Fos.Owin;
using Fos.Tests.Middleware;

namespace Fos.Tests
{
	// A lot to do in the browser class and in the library before we are able to write these methods
	/*
	[TestFixture]
	public class CustomPages
	{
		[Test]
		public void ErrorPage()
		{
			int port = 9007;

			var config = (builder) =>
			{
				builder.Use(typeof(ThrowsExceptionApplication));
			};

			// PageNotFoundMiddleware without a next handler will throw error...
			using (var server = new FCgiOwinSelfHost(config))
			{
				server.Bind(System.Net.IPAddress.Loopback, port);
				server.Start(true);

				using (var browser = new Browser(System.Net.IPAddress.Loopback, port))
				{

				}
			}
		}

		[Test]
		public void HelloWorldResponse()
		{
			var config = (builder) =>
			{
				builder.Use(typeof(HelloWorldApplication));
			};
		}

		[Test]
		public void EmptyResponsePage()
		{
			var config = (builder) =>
			{
				builder.Use(typeof(EmptyResponseApplication));
			};
		}
	}
	*/
}
