using System;
using NUnit.Framework;
using Owin;
using Fos;
using Fos.Owin;
using Fos.Tests.Middleware;
using Fos.CustomPages;
using System.IO;

namespace Fos.Tests
{
	// A lot to do in the browser class and in the library before we are able to write these methods

	[TestFixture]
	public class CustomPages
	{
		private System.Net.IPAddress ListenOn;
		private int ListenPort;
		
		[TestFixtureSetUp]
		public void Setup()
		{
			ListenOn = System.Net.IPAddress.Loopback;
			ListenPort = 9007;
		}

		private string ReadStream(Stream s)
		{
			using (var reader = new StreamReader(s))
			{
				return reader.ReadToEnd();
			}
		}

		/*
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
		}*/


		/*
		[Test]
		public void EmptyResponsePageTest()
		{

			Console.WriteLine("WHAAT");
			Action<IAppBuilder> config = (builder) =>
			{
				builder.Use(typeof(EmptyResponseApplication));
			};

			using (var server = new FosSelfHost(config))
			{
				server.Bind(ListenOn, ListenPort);
				Console.WriteLine("After bind and before start");
				server.Start(true);
				Console.WriteLine("After start");

				// Make the request and expect the empty response page with 500 status code
				var browser = new Browser(ListenOn, ListenPort);
				var response = browser.ExecuteRequest("http://localhost/", "GET");

				var emptyResponsePage = new EmptyResponsePage();

				Assert.AreEqual(500, response.StatusCode);
				Assert.AreEqual(emptyResponsePage.Contents, ReadStream(response.ResponseBody));
			}
		}
		*/
	}

}
