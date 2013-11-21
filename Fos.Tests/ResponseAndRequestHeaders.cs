using System;
using NUnit.Framework;
using Fos;

namespace Fos.Tests
{
    [TestFixture]
    public class ResponseAndRequestHeaders
    {
        [Test]
        public void HeadersValuesAreArrayCopies()
        {
            var headerDict = new HeaderDictionary();
            headerDict.Add("Name", new string[1] { "Value" });

            string[] copy = headerDict["Name"];

            Assert.AreEqual("Value", copy [0]);

            // Change and make sure it is still the same in the original!
            copy [0] = "Something different";

            string[] copy2 = headerDict["Name"];
            Assert.AreEqual("Value", copy2 [0]);
        }

        [Test]
        public void OrdinalInsensitiveKeyComparison()
        {
            var headerDict = new HeaderDictionary();
            headerDict.Add("Name", new string[1] { "Value" });

            Assert.Throws<ArgumentException>(() => headerDict.Add("nAmE", new string[1] { "whatever" }));
        }
    }
}

