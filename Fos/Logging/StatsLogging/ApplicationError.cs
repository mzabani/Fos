using System;

namespace Fos.Logging
{
    internal class ApplicationError
    {
        public string HttpMethod { get; private set; }
        public string RelativePath { get; private set; }

        /// <summary>
        /// All request cookies concatenaded as one big string.
        /// </summary>
        public string Cookies { get; private set; }

        public Exception Error { get; private set; }

        /// <summary>
        /// The time when the application error happened, in UTC.
        /// </summary>
        public DateTime When { get; private set; }

        internal ApplicationError(string verb, string relativePath, Exception e)
        {
            HttpMethod = verb;
            RelativePath = relativePath;
            Error = e;

            //TODO: Cookies

            When = DateTime.UtcNow;
        }
    }
}
