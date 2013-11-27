using System;
using System.Net.Sockets;
using FastCgiNet;
using FastCgiNet.Streams;
    
namespace Fos.Streams
{
    /// <summary>
    /// This is just a stream wrapper over a socket to send FastCgi Stdout Records without ever sending the End-Of-Stream empty record.
    /// It also swallows all socket exceptions.
    /// </summary>
    internal class NonEndingStdoutSocketStream : SocketStream
    {
        /// <summary>
        /// Since this stream will be written to/flushed by the application and the socket may be closed by the time it does so,
        /// we will swallow all SocketExceptions and ObjectDisposedExceptions for the Socket in order not to assume that an exception thrown by Stream.Flush()
        /// is an application error.
        /// </summary>
        public override void Close()
        {
            try
            {
                this.Flush();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
        }

        /// <summary>
        /// Since this stream will be written to/flushed by the application and the socket may be closed by the time it does so,
        /// we will swallow all SocketExceptions and ObjectDisposedExceptions for the Socket in order not to assume that an exception thrown by Stream.Flush()
        /// is an application error.
        /// </summary>
        public override void Flush()
        {
            try
            {
                base.Flush();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
        }

        public NonEndingStdoutSocketStream(Socket sock)
            : base(sock, RecordType.FCGIStdout, false)
        {
        }
    }
}
