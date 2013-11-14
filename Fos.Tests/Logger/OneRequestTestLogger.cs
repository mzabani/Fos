using System;
using Fos.Logging;
using FastCgiNet;
using System.Net.Sockets;

namespace Fos.Tests
{
    public class OneRequestTestLogger : IServerLogger
    {
        public bool ConnectionWasReceived { get; private set; }

        public bool ServerWasStarted { get; private set; }
        public bool ServerWasStopped { get; private set; }

        /// <summary>
        /// True if the socket was closed before it sent any request info at all.
        /// </summary>
        public bool ConnectionClosedAbruptlyWithoutAnyRequestInfo { get; private set; }

        /// <summary>
        /// If there was a socket error, this points to it.
        /// </summary>
        public Exception SocketError { get; private set; }

        /// <summary>
        /// Connection was closed only after the request was completed. Everything went fine.
        /// </summary>
        public bool ConnectionClosedNormally { get; private set; }

        /// <summary>
        /// The request info for the request, if any was logged.
        /// </summary>
        public RequestInfo RequestInfo { get; private set; }

        /// <summary>
        /// The exception thrown by the application, if any.
        /// </summary>
        public Exception ApplicationError { get; private set; }

        public void ServerStart()
        {
            ServerWasStarted = true;
        }

        public void ServerStop()
        {
            ServerWasStopped = true;
        }

        public void LogConnectionReceived(Socket createdSocket)
        {
            ConnectionWasReceived = true;
        }

        public void LogConnectionClosedAbruptly(Socket s, RequestInfo req)
        {
            if (req == null)
                ConnectionClosedAbruptlyWithoutAnyRequestInfo = true;

            RequestInfo = req;
        }

        public void LogConnectionEndedNormally(Socket s, RequestInfo req)
        {
            ConnectionClosedNormally = true;
            RequestInfo = req;
        }

        public void LogApplicationError(Exception e, RequestInfo req)
        {
            ApplicationError = e;
            RequestInfo = req;
        }

        public void LogServerError(Exception e, string format, params object[] prms)
        {
        }

        public void LogSocketError(Socket s, Exception e, string format, params object[] prms)
        {
            SocketError = e;
        }

        public void LogInvalidRecordReceived(RecordBase invalidRecord)
        {
        }

        public OneRequestTestLogger()
        {
        }
    }
}
