using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Fos
{
	/// <summary>
	/// This special stream has a way of telling its caller when a first write happens, and also waits
	/// until streams fill up to 65535 bytes (FastCgi's content size limit of a record), then instantiating
	/// a new Stream of type <typeparamref name="T" /> and writing to it after that, and so on.
	/// </summary>
	internal class FragmentedRequestStream<T> : Stream
		where T : Stream, new()
	{
		private LinkedList<T> underlyingStreams;
		private long position;
		private T lastStream;

		public IEnumerable<T> UnderlyingStreams
		{
			get
			{
				return underlyingStreams;
			}
		}

		public void AppendStream(T s)
		{
			if (s == null)
				throw new ArgumentNullException("s");
			else if (!s.CanRead || !s.CanSeek)
				throw new ArgumentException("The stream has to be seekable and readable");

			underlyingStreams.AddLast(s);
			lastStream = s;
		}

		#region Implemented abstract members of Stream
		public override void Flush()
		{
			foreach (var stream in underlyingStreams)
				stream.Flush();
		}
		public override int Read(byte[] buffer, int offset, int count)
		{
			long bytesSkipped = 0;
			int totalBytesRead = 0;
			foreach (var stream in underlyingStreams)
			{
				// Only start reading in the first stream that contains data according to the stream's current position
				if (bytesSkipped + stream.Length - 1 < position)
				{
					bytesSkipped += stream.Length;
					continue;
				}

				// If Read gets called more than once, we may be reading from an advanced stream, so rewind it
				stream.Seek(0, SeekOrigin.Begin);

				// If this is the first stream, skip bytes that we don't want.
				// We must read either up to "count" bytes or the entire current stream, whichever one is smaller.
				int bytesToRead = count - totalBytesRead;
				if (bytesToRead == 0)
					break;

				if (totalBytesRead == 0)
				{
					stream.Seek(position - bytesSkipped, SeekOrigin.Begin);
					if (stream.Length - (position - bytesSkipped) < bytesToRead)
						bytesToRead = (int)(stream.Length - (position - bytesSkipped));
				}
				else if (bytesToRead > stream.Length)
					bytesToRead = (int)stream.Length;

				totalBytesRead += stream.Read(buffer, offset + totalBytesRead, bytesToRead);
			}

			position += totalBytesRead;
			return totalBytesRead;
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
			//TODO: This is very wrong. We should seek all positions starting from the last to zero until we find the one that
			// won't be rewinded completely, plus it disconsiders "origin"
			lastStream.Position = offset - underlyingStreams.Take(underlyingStreams.Count - 1).Sum(s => s.Length);
		}
		public override void SetLength (long value)
		{
			throw new NotSupportedException();
		}
		public override void Write (byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
		public override bool CanRead {
			get {
				return true;
			}
		}
		public override bool CanSeek {
			get {
				return false;
			}
		}
		public override bool CanWrite {
			get
			{
				return false;
			}
		}
		public override long Length {
			get
			{
				return underlyingStreams.Sum(s => s.Length);
			}
		}
		public override long Position {
			get
			{
				return position;
			}
			set
			{
				Seek(value, SeekOrigin.Begin);
			}
		}
		#endregion

		public FragmentedRequestStream()
		{
			underlyingStreams = new LinkedList<T>();
			position = 0;
		}
	}
}
