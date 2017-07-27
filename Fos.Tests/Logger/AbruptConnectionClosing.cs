using System;
using Owin;
using NUnit.Framework;
using FastCgiNet;
using Fos;
using Fos.Logging;
using System.Net.Sockets;
using Fos.Listener;

namespace Fos.Tests
{
    [TestFixture]
    public class AbruptConnectionClosing : SocketTests
    {
        [Ignore("CloseConnectionAbruptlyBeforeSendingAnyRecord")]
        public void CloseConnectionAbruptlyBeforeSendingAnyRecord()
        {
            var logger = new OneRequestTestLogger();

            using (var server = GetHelloWorldBoundServer())
            {
                server.SetLogger(logger);
                server.Start(true);

                // Just connect and quit
                using (var sock = ConnectAndGetSocket())
                {
                }

                Assert.IsFalse(logger.ServerError);
                Assert.IsTrue(logger.ConnectionWasReceived);
                Assert.IsFalse(logger.ConnectionClosedNormally);
                //TODO: NOT working yet: Assert.IsTrue(logger.ConnectionClosedAbruptlyWithoutUrl);
            }
        }

        [Test]
        public void CloseConnectionAbruptlyAfterSendingBeginRequestRecord()
        {
            var logger = new OneRequestTestLogger();
            
            using (var server = GetHelloWorldBoundServer())
            {
                server.SetLogger(logger);
                server.Start(true);
                
                // Just connect and quit
                using (var webServer = ConnectAndGetWebServerRequest(1))
                {
                    webServer.SendBeginRequest(Role.Responder, true);
                }
                
                //TODO: Can't we do better than this?
                System.Threading.Thread.Sleep(10);

                Assert.IsFalse(logger.ServerError);
                Assert.IsTrue(logger.ConnectionWasReceived);
                Assert.IsFalse(logger.ConnectionClosedNormally);
                //TODO: NOT working yet: Assert.IsTrue(logger.ConnectionClosedAbruptlyWithoutUrl);
            }
        }

        [Ignore("CloseConnectionAbruptlyAfterSendingIncompleteDataWithoutEmptyParamsRecord")]
        public void CloseConnectionAbruptlyAfterSendingIncompleteDataWithoutEmptyParamsRecord()
        {
        }

        [Ignore("CloseConnectionAbruptlyAfterSendingIncompleteDataWithEmptyParamsRecord")]
        public void CloseConnectionAbruptlyAfterSendingIncompleteDataWithEmptyParamsRecord()
        {
        }
    }
}
