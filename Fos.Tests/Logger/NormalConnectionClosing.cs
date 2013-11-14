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

                Assert.AreEqual(true, logger.ServerWasStarted);
                Assert.AreEqual(false, logger.ServerWasStopped);
            }

            Assert.AreEqual(true, logger.ServerWasStopped);
            Assert.AreEqual(false, logger.ConnectionWasReceived);
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
                var browser = new Browser(ListenOn, ListenPort);
                browser.ExecuteRequest("http://localhost/", "GET");
            }
            
            Assert.AreEqual(true, logger.ConnectionWasReceived);
            Assert.AreEqual(true, logger.ConnectionClosedNormally);
            Assert.That(logger.RequestInfo != null && logger.RequestInfo.RelativePath == "/" && logger.RequestInfo.ResponseStatusCode== 200);
            Assert.AreEqual(false, logger.ConnectionClosedAbruptlyWithoutAnyRequestInfo);
        }
    }
}
