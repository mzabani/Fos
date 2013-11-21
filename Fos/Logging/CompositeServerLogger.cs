using System;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Fos.Logging
{
    /// <summary>
    /// When you want to use more than one logger, use this class to wrap them.
    /// </summary>
    public class CompositeServerLogger : IServerLogger
    {
        private LinkedList<IServerLogger> Loggers;

        /// <summary>
        /// Adds a logger to this composite logger. This means this logger's methods will be invoked by the server to log
        /// its actions.
        /// </summary>
        public void AddLogger(IServerLogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException("logger");

            Loggers.AddLast(logger);
        }

        public void ServerStart()
        {
            foreach (var logger in Loggers)
                logger.ServerStart();
        }

        public void ServerStop()
        {
            foreach (var logger in Loggers)
                logger.ServerStop();
        }

        public void LogConnectionReceived(Socket createdSocket)
        {
            foreach (var logger in Loggers)
                logger.LogConnectionReceived(createdSocket);
        }

        public void LogConnectionClosedAbruptly(Socket s, RequestInfo req)
        {
            foreach (var logger in Loggers)
                logger.LogConnectionClosedAbruptly(s, req);
        }

        public void LogConnectionEndedNormally(Socket s, RequestInfo req)
        {
            foreach (var logger in Loggers)
                logger.LogConnectionEndedNormally(s, req);
        }

        public void LogApplicationError(Exception e, RequestInfo req)
        {
            foreach (var logger in Loggers)
                logger.LogApplicationError(e, req);
        }

        public void LogServerError(Exception e, string format, params object[] prms)
        {
            foreach (var logger in Loggers)
                logger.LogServerError(e, format, prms);
        }

        public void LogSocketError(Socket s, Exception e, string format, params object[] prms)
        {
            foreach (var logger in Loggers)
                logger.ServerStart();
        }

        public void LogInvalidRecordReceived(FastCgiNet.RecordBase invalidRecord)
        {
            foreach (var logger in Loggers)
                logger.LogInvalidRecordReceived(invalidRecord);
        }

        public CompositeServerLogger()
        {
            Loggers = new LinkedList<IServerLogger>();
        }
    }
}
