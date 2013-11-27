using System;
using System.Linq;
using System.Net.Sockets;

namespace Fos
{
    internal class SocketHelper
    {
        private static readonly SocketError[] ConnectionClosedErrors = new SocketError[] { SocketError.Interrupted, SocketError.ConnectionReset, SocketError.ConnectionAborted };

        /// <summary>
        /// Determines if the exception thrown is a <see cref="SocketException"/> meaning that the connection was closed by the other side.
        /// </summary>
        public static bool IsConnectionAbortedByTheOtherSide(SocketException socketEx)
        {
            if (socketEx == null)
                throw new ArgumentNullException("socketEx");

            return ConnectionClosedErrors.Contains(socketEx.SocketErrorCode);
        }
    }
}
