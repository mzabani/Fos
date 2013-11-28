using System;
using Owin;
using NUnit.Framework;
using Fos;
using Fos.Logging;
using System.Net.Sockets;

namespace Fos.Tests
{
    [TestFixture]
    public class NormalConnectionClosing : SocketTests
    {
        [Test]
        public void StartAndStop()
        {
            var logger = new OneRequestTestLogger();
            
            using (var server = GetHelloWorldBoundServer())
            {
                server.SetLogger(logger);
                server.Start(true);

                Assert.IsTrue(logger.ServerWasStarted);
                Assert.IsFalse(logger.ServerWasStopped);
            }

            Assert.IsTrue(logger.ServerWasStopped);
            Assert.IsFalse(logger.ConnectionWasReceived);
        }

        [Test]
        public void ApplicationErrorLogging()
        {
            var logger = new OneRequestTestLogger();
            
            using (var server = GetApplicationErrorBoundServer())
            {
                server.SetLogger(logger);
                server.Start(true);
                
                // A Request for the root dir /
                var browser = new Browser(ListenOn, ListenPort);
                browser.ExecuteRequest("http://localhost/", "GET");
            }
            
            Assert.IsNotNull(logger.ApplicationError);
            Assert.That(logger.ApplicationError.ToString().Contains("An error occured in the application. On purpose."));
            Assert.That(logger.RequestInfo != null && logger.RequestInfo.RelativePath == "/" && logger.RequestInfo.ResponseStatusCode == 500);
        }

        [Test]
        public void CheckBasicData()
        {
            var logger = new OneRequestTestLogger();
            
            using (var server = GetHelloWorldBoundServer())
            {
                server.SetLogger(logger);
                server.Start(true);
                
                // A Request for the root dir /
                using (var browser = new Browser(ListenOn, ListenPort))
                {
                    browser.ExecuteRequest("http://localhost/", "GET");
                }

                Assert.IsTrue(logger.ConnectionWasReceived);
                Assert.IsTrue(logger.ConnectionClosedNormally);
                Assert.That(logger.RequestInfo != null && logger.RequestInfo.RelativePath == "/" && logger.RequestInfo.ResponseStatusCode== 200);
                Assert.IsFalse(logger.ConnectionClosedAbruptlyWithoutUrl);
            }
        }
    }
}
