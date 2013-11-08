using System;
using System.Linq;
using NUnit.Framework;
using Fos;
using System.IO;

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
				
				// Now read stuff. Set an offset different than 0
				byte[] newBuf = new byte[16];
				int offset = 10;
				Assert.AreEqual(1, s.Read(newBuf, offset, 1));
				Assert.AreEqual(1, s.Position);
				Assert.AreEqual(data, newBuf[offset]);
			}
		}

		[Test]
		public void ReadCheckBytePerByte()
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
				
				for (int i = 0; i < superBuf.Length; ++i)
				{
					Assert.AreEqual(1, s.Read(buf, 0, 1));
					Assert.AreEqual((byte)(i % 10), buf[0]);
				}
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
