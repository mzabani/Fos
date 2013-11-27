using System;
using System.Linq;
using NUnit.Framework;
using Fos;
using Fos.Streams;
using System.IO;

namespace Fos.Tests
{
	[TestFixture]
    public class HeaderWriterTests
	{
		[Test]
		public void OnFirstWriteAndOnStreamFillEvents()
		{
            byte[] buf = new byte[1024];
            using (var s = new MemoryStream(buf))
            {
                using (var writer = new HeaderWriter(s))
                {
                    writer.Write("Status", "200");
                    writer.WriteLine();
                }
            }

            int writtenLength = "Status: 200\r\n\r\n".Length;
            var contents = System.Text.ASCIIEncoding.ASCII.GetString(buf, 0, writtenLength);
            Assert.AreEqual("Status: 200\r\n\r\n", contents);
		}
	}
}
