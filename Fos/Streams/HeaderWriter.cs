using System;
using System.IO;

namespace Fos.Streams
{
    /// <summary>
    /// A TextWriter that helps write HTTP Headers to a stream.
    /// </summary>
    internal class HeaderWriter : StreamWriter
    {
        public void Write(string headerName, string headerValue)
        {
            Write(headerName);
            Write(": ");
            WriteLine(headerValue);
        }

        public void Write(string headerName, string[] headerValue)
        {
            Write(headerName);
            Write(": ");
            
            for (int i = 0; i < headerValue.Length; ++i)
            {
                Write(headerValue[i]);
                
                // If it is not the last, add a comma
                if (i < headerValue.Length - 1)
                    Write(", ");
            }
            
            WriteLine();
        }

        public HeaderWriter(Stream s)
            : base(s, System.Text.Encoding.ASCII)
        {
            this.NewLine = "\r\n";
        }
    }
}
