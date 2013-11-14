using System;
using Owin;
using NUnit.Framework;
using Fos;
using Fos.Logging;
using System.Net.Sockets;

namespace Fos.Tests
{
    [TestFixture]
    public class AbruptConnectionClosing : SocketTests
    {
        [Ignore]
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
                    sock.Close();
                }

                System.Threading.Thread.Sleep(10);
            }

            Assert.AreEqual(true, logger.ConnectionWasReceived);
            Assert.AreEqual(false, logger.ConnectionClosedNormally);
            Assert.AreEqual(null, logger.RequestInfo);
            Assert.AreEqual(true, logger.ConnectionClosedAbruptlyWithoutAnyRequestInfo);
        }

        [Ignore]
        public void CloseConnectionAbruptlyAfterSendingBeginRequestRecord()
        {
        }

        [Ignore]
        public void CloseConnectionAbruptlyAfterSendingIncompleteDataWithoutEmptyParamsRecord()
        {
        }

        [Ignore]
        public void CloseConnectionAbruptlyAfterSendingIncompleteDataWithEmptyParamsRecord()
        {
        }
    }
}
