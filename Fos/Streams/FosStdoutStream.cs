using System;
using System.Net.Sockets;
using FastCgiNet;
using FastCgiNet.Streams;
    
namespace Fos.Streams
{
    delegate void StreamWriteEvent();

    delegate void StreamFilledEvent();

    /// <summary>
    /// Stream that contains special events such as OnFirstWrite and OnStreamFill. It is also remarkable in that it completely swallows
    /// exceptions thrown by socket operations.
    /// </summary>
    internal class FosStdoutStream : SocketStream
    {
        /// <summary>
        /// This event is triggered on the very first write to this Stream.
        /// </summary>
        public event StreamWriteEvent OnFirstWrite = delegate {};
        private bool FirstWrite;

        /// <summary>
        /// Since this stream will be written to/flushed by the application and the socket may be closed by the time it does so,
        /// we will swallow all SocketExceptions and ObjectDisposedExceptions for the Socket in order not to assume that an exception thrown by Stream.Flush()
        /// is an application error.
        /// </summary>
        public override void Close()
        {
            try
            {
                base.Close();
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

        /// <summary>
        /// Sign up to be informed when a Stream fills up all of the 65535 bytes that limit the size of a FastCgi Record's contents.
        /// This event will be triggered only once per filled stream.
        /// </summary>
        public event StreamFilledEvent OnStreamFill = delegate {};
        public override void AppendStream(RecordContentsStream stream)
        {
            // If a stream is appended, it means that the last one filled up (except for the very first one)!
            if (underlyingStreams.Count > 0)
                OnStreamFill();

            base.AppendStream(stream);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);
            if (!FirstWrite)
            {
                FirstWrite = true;
                OnFirstWrite();
            }
        }

        public FosStdoutStream(Socket sock)
            : base(sock, RecordType.FCGIStdout, false)
        {
            FirstWrite = false;
        }
    }
}
