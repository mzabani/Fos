using System;
using System.Linq;
using NUnit.Framework;
using FastCgiServer;
using System.IO;

namespace FastCgiServer.Tests
{
	[TestFixture]
	public class Test
	{
		[Test]
		public void OnFirstWriteAndOnStreamFillEvents()
		{
			using (var s = new FragmentedResponseStream<MemoryStream>())
			{
				int numFirstWrites = 0;
				s.OnFirstWrite += () => { numFirstWrites++; };

				int numStreamFills = 0;
				Stream lastFilledStream = null;
				s.OnStreamFill += (filledStream) => {
					lastFilledStream = filledStream;
					numStreamFills++;
				};

				byte[] data = new byte[1024];
				Assert.AreEqual(0, numFirstWrites);
				s.Write(data, 0, 1024);
				Assert.AreEqual(1, numFirstWrites);
				s.Write(data, 0, 1024);
				Assert.AreEqual(1, numFirstWrites);

				// All right.. fill some streams!
				int numFilledStreams = 5;
				int chunkSize = 65535 * numFilledStreams + 1;
				byte[] hugeChunk = new byte[chunkSize];
				Assert.AreEqual(null, lastFilledStream);
				Assert.AreEqual(0, numStreamFills);
				s.Write(hugeChunk, 0, chunkSize);
				Assert.AreEqual(numFilledStreams, numStreamFills);
			}
		}

		[Test]
		public void LengthAndNumberOfStreamsCheck()
		{
			using (var s = new FragmentedResponseStream<MemoryStream>())
			{
				int numStreams = 3;

				int chunkSize = 65535 * numStreams;
				byte[] hugeChunk = new byte[chunkSize];
				Assert.AreEqual(0, s.Length);
				s.Write(hugeChunk, 0, chunkSize);
				Assert.AreEqual(numStreams, s.UnderlyingStreams.Count());
				Assert.AreEqual(65535, s.LastUnfilledStream.Length);
				Assert.AreEqual(chunkSize, s.Length);
				s.Write(hugeChunk, 0, 1);
				Assert.AreEqual(numStreams + 1, s.UnderlyingStreams.Count ());
				Assert.AreEqual(1, s.LastUnfilledStream.Length);
				Assert.AreEqual(chunkSize + 1, s.Length);
			}
		}
	}
}
