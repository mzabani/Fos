using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Fos
{
	delegate void StreamWriteEvent();

	delegate void StreamFilledEvent<T>(T s);

	/// <summary>
	/// This special stream has a way of telling its caller when a first write happens, and also waits
	/// until streams fill up to 65535 bytes (FastCgi's content size limit of a record), then instantiating
	/// a new Stream of type <typeparamref name="T" /> and writing to it after that, and so on.
	/// </summary>
	internal class FragmentedResponseStream<T> : Stream
		where T : Stream, new()
	{
		const int maxStreamLength = 65535;
		LinkedList<T> underlyingStreams;
		T lastStream;
		bool hasBeenWrittenTo;

		public IEnumerable<T> UnderlyingStreams
		{
			get
			{
				return underlyingStreams;
			}
		}

		/// <summary>
		/// The last unfilled stream that contains part of the response. If this stream has length zero, then this stream
		/// has never been written to.
		/// </summary>
		public T LastUnfilledStream
		{
			get
			{
				return lastStream;
			}
		}

		#region Implemented abstract members of Stream
		public override void Flush ()
		{
			foreach (var stream in underlyingStreams)
				stream.Flush();
		}
		public override int Read (byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException("Read must advance some form of state in here.. not ready yet");

			/*
			int firstStreamIdx, firstStreamOffset, firstStreamCount;

			firstStreamIdx = offset / maxStreamLength;
			firstStreamOffset = offset % maxStreamLength;
			firstStreamCount = maxStreamLength - firstStreamOffset;
			if (firstStreamCount > count)
				firstStreamCount = count;

			using (var streamEnumerator = underlyingStreams.GetEnumerator())
			{
				for (int i = 0; i <= firstStreamIdx; ++i)
					streamEnumerator.MoveNext();

				int totalBytesRead = 0;
				T firstStream = streamEnumerator.Current;
				firstStream.Read(buffer, firstStreamOffset, firstStreamCount);
				totalBytesRead += firstStreamCount;

				while (totalBytesRead != count)
				{
					if (!streamEnumerator.MoveNext())
						throw new ArgumentException("You are reading too much. We don't have all these bytes available");

					T stream = streamEnumerator.Current;
					int bytesToRead = count - totalBytesRead;
					if (bytesToRead > maxStreamLength)
						bytesToRead = maxStreamLength;
					stream.Read(buffer, totalBytesRead, bytesToRead);
					totalBytesRead += bytesToRead;
				}
			}*/
		}
		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}
		public override void SetLength (long value)
		{
			throw new NotImplementedException();
		}
		public override void Write (byte[] buffer, int offset, int count)
		{
			int bytesCopied = 0;
			while (bytesCopied != count)
			{
				int bytesToCopy = maxStreamLength - (int)lastStream.Length;
				if (bytesToCopy > count - bytesCopied)
					bytesToCopy = count - bytesCopied;

				if (bytesToCopy > 0)
				{
					lastStream.Write(buffer, offset + bytesCopied, bytesToCopy);
					bytesCopied += bytesToCopy;
					if (!hasBeenWrittenTo)
					{
						hasBeenWrittenTo = true;
						OnFirstWrite();
					}
				}

				if (lastStream.Length == maxStreamLength && bytesCopied < count)
				{
					OnStreamFill(lastStream);

					// New lastStream
					lastStream = new T();
					underlyingStreams.AddLast(lastStream);
				}
			}
		}
		public override bool CanRead {
			get {
				return lastStream.CanRead;
			}
		}
		public override bool CanSeek {
			get {
				return lastStream.CanSeek;
			}
		}
		public override bool CanWrite {
			get {
				return lastStream.CanWrite;
			}
		}
		public override long Length {
			get {
				return underlyingStreams.Sum(s => s.Length);
			}
		}
		public override long Position {
			get {
				return underlyingStreams.Take(underlyingStreams.Count - 1).Sum(s => s.Length) + lastStream.Position;
			}
			set {
				lastStream.Position = value - underlyingStreams.Take(underlyingStreams.Count - 1).Sum(s => s.Length);
			}
		}
		#endregion

		/// <summary>
		/// This event is triggered on the very first write to this Stream.
		/// </summary>
		public event StreamWriteEvent OnFirstWrite;

		/// <summary>
		/// Sign up to be informed when a Stream fills up all of the 65535 bytes that limit the size of a FastCgi record's contents.
		/// The stream passed as argument is the filled stream. This event will be triggered only once per filled stream.
		/// </summary>
		public event StreamFilledEvent<T> OnStreamFill;

		public FragmentedResponseStream ()
		{
			underlyingStreams = new LinkedList<T>();
			lastStream = new T();
			underlyingStreams.AddLast(lastStream);
			hasBeenWrittenTo = false;
			OnFirstWrite = delegate {};
			OnStreamFill = delegate {};
		}
	}
}
