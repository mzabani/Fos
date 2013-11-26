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

                //TODO: Can't we do better than this?
                System.Threading.Thread.Sleep(10);
            }

            Assert.IsTrue(logger.ConnectionWasReceived);
            Assert.IsFalse(logger.ConnectionClosedNormally);
            Assert.IsNull(logger.RequestInfo);
            Assert.IsTrue(logger.ConnectionClosedAbruptlyWithoutAnyRequestInfo);
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
                using (var sock = ConnectAndGetSocket())
                {
                    var beginReq = new BeginRequestRecord(1);
                    using (var req = new FosRequest(sock, logger))
                    {
                        req.SetBeginRequest(beginReq);
                        req.Send(beginReq);
                    }
                }
                
                //TODO: Can't we do better than this?
                System.Threading.Thread.Sleep(10);
            }
            
            Assert.IsTrue(logger.ConnectionWasReceived);
            Assert.IsFalse(logger.ConnectionClosedNormally);
            Assert.IsNotNull(logger.RequestInfo);
            Assert.IsFalse(logger.ConnectionClosedAbruptlyWithoutAnyRequestInfo);
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
