using System;
using NUnit.Framework;

namespace Fos.Tests
{
    [TestFixture]
    public class AbruptConnectionClosing
    {
        [Ignore]
        public void CloseConnectionAbruptelyBeforeSendingAnyRecord()
        {
        }

        [Ignore]
        public void CloseConnectionAbruptelyAfterSendingBeginRequestRecord()
        {
        }

        [Ignore]
        public void CloseConnectionAbruptelyAfterSendingIncompleteDataWithoutEmptyParamsRecord()
        {
        }

        [Ignore]
        public void CloseConnectionAbruptelyAfterSendingIncompleteDataWithEmptyParamsRecord()
        {
        }

        [Ignore]
        public void CloseConnectionAbruptelyAfterSendingCompleteData()
        {
        }
    }
}

