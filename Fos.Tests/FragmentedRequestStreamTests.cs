using System;
using System.Linq;
using NUnit.Framework;
using Fos;
using System.IO;
using FastCgiNet;

namespace Fos.Tests
{
	[TestFixture]
	public class FragmentedRequestStreamTests
	{
		[Test]
		public void ReadCheckOneStreamOneByteOffsetDifferentThanZero()
		{
			using (var s = new FragmentedRequestStream<MemoryStream>())
			{
				var stream = new MemoryStream();

				byte[] buf = new byte[1];
				byte data = (byte)93;
				buf[0] = data;

				stream.Write(buf, 0, 1);
				stream.Seek(0, SeekOrigin.Begin);
				s.AppendStream(stream);
				
				// Now read stuff. Set an offset to something different than 0
				byte[] newBuf = new byte[16];
				int offset = 10;
				Assert.AreEqual(1, s.Read(newBuf, offset, 1));
				Assert.AreEqual(1, s.Position);
				Assert.AreEqual(data, newBuf[offset]);
			}
		}

		[Test]
		public void ReadTwoStreamsOneByteEachWithSeeking()
		{
			using (var s = new FragmentedRequestStream<MemoryStream>())
			{
				var streams = new MemoryStream[2];

				byte[] buf = new byte[2];
				byte b0 = (byte)32;
				byte b1 = (byte)33;
				buf[0] = b0;
				buf[1] = b1;
				streams[0] = new MemoryStream();
				streams[0].Write(buf, 0, 1);
				streams[1] = new MemoryStream();
				streams[1].Write(buf, 1, 1);

				// Add streams to the FragmentedStream
				s.AppendStream(streams[0]);
				s.AppendStream(streams[1]);
				
				// Now scramble the buffer and read those bytes
				buf[0] = (byte)0;
				buf[1] = (byte)0;

				// Make sure seeking works
				Assert.AreEqual(0, s.Position);
				for (int i = 0; i < 2; ++i)
				{
					s.Read(buf, 0, 1);
					s.Read(buf, 1, 1);
					Assert.AreEqual(b0, buf[0]);
					Assert.AreEqual(b1, buf[1]);
					s.Position = 0;
				}
			}
		}

		[Test]
		public void ReadManyStreamsBytePerByte()
		{
			using (var s = new FragmentedRequestStream<MemoryStream>())
			{
				int numStreams = 3;
				
				var streams = new MemoryStream[numStreams];
				
				// Write numStreams streams with 10 bytes each, storing sequential numbers
				byte[] buf = new byte[10];
				for (int i = 0; i < buf.Length; ++i)
					buf[i] = (byte)i;
				
				for (int i = 0; i < numStreams; ++i)
				{
					streams[i] = new MemoryStream();
					streams[i].Write(buf, 0, 10);
					s.AppendStream(streams[i]);
				}
				
				// Now read stuff
				byte[] superBuf = new byte[numStreams * 10];

				for (int i = 0; i < buf.Length * numStreams; ++i)
				{
					Assert.AreEqual(30, 30 * s.Read(buf, 0, 1));
					Assert.AreEqual((byte)(i % 10), buf[0]);
				}

				for (int i = 0; i < numStreams; ++i)
					streams[0].Dispose();
			}
		}

		[Test]
		public void ReadCheckAllAtOnce()
		{
			using (var s = new FragmentedRequestStream<MemoryStream>())
			{
				int numStreams = 3;
				
				var streams = new MemoryStream[numStreams];
				
				// Write numStreams streams with 10 bytes each, storing sequential numbers
				byte[] buf = new byte[10];
				for (int i = 0; i < buf.Length; ++i)
					buf[i] = (byte)i;
				
				for (int i = 0; i < numStreams; ++i)
				{
					streams[i] = new MemoryStream();
					streams[i].Write(buf, 0, 10);
					streams[i].Seek(0, SeekOrigin.Begin);
					s.AppendStream(streams[i]);
				}
				
				// Now read stuff
				byte[] superBuf = new byte[numStreams * 10];

				Assert.AreEqual(superBuf.Length, s.Read(superBuf, 0, superBuf.Length));
				Assert.AreEqual(superBuf.Length, s.Position);

				for (int i = 0; i < superBuf.Length; ++i)
				{
					Assert.AreEqual(buf[i % 10], superBuf[i]);
				}
			}
		}
	}
}
