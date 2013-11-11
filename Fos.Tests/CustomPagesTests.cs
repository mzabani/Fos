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
	[TestFixture]
	public class CustomPagesTests
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
		public void ErrorPageTest()
		{
			Action<IAppBuilder> config = (builder) =>
			{
				builder.Use(typeof(EmptyResponseApplication));
			};
			
			using (var server = new FosSelfHost(config))
			{
				server.Bind(ListenOn, ListenPort);
				server.Start(true);
				
				// Make the request and expect the empty response page with 500 status code
				var browser = new Browser(ListenOn, ListenPort);
				var response = browser.ExecuteRequest("http://localhost/", "GET");

				var errorPage = new ApplicationErrorPage(new Exception("An error occured in the application. On purpose."));

				Assert.AreEqual(500, response.StatusCode);
				Assert.AreEqual(errorPage.Contents, ReadStream(response.ResponseBody));
			}
		}*/

		/*
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
			Action<IAppBuilder> config = (builder) =>
			{
				builder.Use(typeof(EmptyResponseApplication));
			};

			using (var server = new FosSelfHost(config))
			{
				server.Bind(ListenOn, ListenPort);
				server.Start(true);

				System.Threading.Thread.Sleep(100);

				// Make the request and expect the empty response page with 500 status code
				var browser = new Browser(ListenOn, ListenPort);
				using (var response = browser.ExecuteRequest("http://localhost/", "GET"))
				{
					var emptyResponsePage = new EmptyResponsePage();

					Assert.AreEqual(500, response.StatusCode);
					Assert.AreEqual(emptyResponsePage.Contents, ReadStream(response.ResponseBody));
				}
			}
		}*/
	}
}
