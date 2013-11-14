using System;
using NUnit.Framework;
using Fos;
using Fos.Owin;
using FastCgiNet;
using System.Threading;

namespace Fos.Tests
{
	[TestFixture]
	public class OwinContextTests
	{
		private CancellationTokenSource TokenSource;

		[TestFixtureSetUp]
		public void Setup()
		{
			TokenSource = new CancellationTokenSource();
		}

		[Test]
		public void UriCheck()
		{
			var ctx = new OwinContext("1.0", TokenSource.Token);

			NameValuePair docUri = new NameValuePair("DOCUMENT_URI", "/about/terms");
			NameValuePair host = new NameValuePair("HTTP_HOST", "localhost");
			ctx.SetOwinParametersFromFastCgiNvp(docUri);
			ctx.SetOwinParametersFromFastCgiNvp(host);

			Assert.AreEqual("http://localhost/about/terms", ctx.CompleteUri);
			Assert.AreEqual(string.Empty, (string)ctx["owin.RequestPathBase"]);
			Assert.AreEqual("/about/terms", (string)ctx["owin.RequestPath"]);
		}

        [Test]
        public void PartialContextNoMethod()
        {
            var ctx = new OwinContext("1.0", TokenSource.Token);

            Assert.AreEqual(false, ctx.HttpMethodDefined);
        }

        [Test]
        public void PartialContextNoUri()
        {
            var ctx = new OwinContext("1.0", TokenSource.Token);
            
            Assert.AreEqual(false, ctx.RelativePathDefined);
        }

        [Test]
        public void PartialContextNoResponse()
        {
            var ctx = new OwinContext("1.0", TokenSource.Token);
            
            Assert.AreEqual(false, ctx.SomeResponseExists);
        }
	}
}
