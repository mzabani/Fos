using System;
using System.Linq;
using NUnit.Framework;
using Fos;
using Fos.Streams;
using System.IO;

namespace Fos.Tests
{
	[TestFixture]
    public class StdoutStream : SocketTests
	{
		[Test]
		public void OnFirstWriteAndOnStreamFillEvents()
		{
            using (var server = this.GetHelloWorldBoundServer())
            {
                server.Start(true);

                using (var sock = this.ConnectAndGetSocket())
                {
                    using (var s = new FosStdoutStream(sock))
                    {
                        int numFirstWrites = 0;
                        s.OnFirstWrite += () => {
                            numFirstWrites++; };
                        
                        int numStreamFills = 0;
                        s.OnStreamFill += () => {
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
                        Assert.AreEqual(0, numStreamFills);
                        s.Write(hugeChunk, 0, chunkSize);
                        Assert.AreEqual(numFilledStreams, numStreamFills);
                    }
                }
            }
		}
	}
}
