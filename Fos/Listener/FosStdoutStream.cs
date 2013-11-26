using System;
using System.Net.Sockets;
using FastCgiNet;
using FastCgiNet.Streams;
    
namespace Fos.Listener
{
    delegate void StreamWriteEvent();

    delegate void StreamFilledEvent(RecordContentsStream stream);

    /// <summary>
    /// Stream that contains special events such as OnFirstWrite and OnStreamFill.
    /// </summary>
    internal class FosStdoutStream : SocketStream
    {
        /// <summary>
        /// This event is triggered on the very first write to this Stream.
        /// </summary>
        public event StreamWriteEvent OnFirstWrite = delegate {};
        private bool FirstWrite;

        /// <summary>
        /// Sign up to be informed when a Stream fills up all of the 65535 bytes that limit the size of a FastCgi Record's contents.
        /// The stream passed as argument is the filled stream. This event will be triggered only once per filled stream.
        /// </summary>
        public event StreamFilledEvent OnStreamFill = delegate {};
        public override void AppendStream(RecordContentsStream stream)
        {
            // If a stream is appended, it means that the last one filled up!
            OnStreamFill(LastUnfilledStream);

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
