using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Fos.Tests
{
	class BrowserResponse : IDisposable
	{
        public int AppStatusCode { get; private set; }
		public int HttpStatusCode { get; private set; }
		public string HttpStatusReason { get; private set; }
		public IDictionary<string, string> Headers { get; private set; }
		public Stream ResponseBody { get; private set; }

		public BrowserResponse(int appStatusCode, Stream response)
		{
			if (response == null)
				throw new ArgumentNullException ("response");

			ResponseBody = new MemoryStream();

			using (var reader = new StreamReader(response, System.Text.ASCIIEncoding.ASCII))
			{
				// First line is status code.. something quick and dirty will do
				string line = reader.ReadLine();
				int spaceIdx = line.IndexOf(' ');
				HttpStatusCode = int.Parse(line.Substring(spaceIdx + 1, 3));
				HttpStatusReason = line.Substring(spaceIdx + 4);

				// Reads the headers
				Headers = new Dictionary<string, string>();
				while ((line = reader.ReadLine()) != string.Empty)
				{
					spaceIdx = line.IndexOf(' ');
                    Console.WriteLine(line);
					string headerName = line.Substring(0, spaceIdx - 1);
					string headerValue = line.Substring(spaceIdx + 1);
					//Console.WriteLine("{0}: {1}", headerName, headerValue);
					Headers.Add(headerName, headerValue);
				}

    			Console.WriteLine ("4");

    			// TODO: Why do we need to do this? Something is very strange here!
    			response.Position = Headers.Sum (h => h.Key.Length + h.Value.Length + 4) + 2;
    			response.Seek("Status: ".Length + HttpStatusCode.ToString().Length + (HttpStatusReason == null ? 0 : HttpStatusReason.Length) + 2, SeekOrigin.Current);
    			// Write the response body and rewind the stream
    			byte[] buf = new byte[4096];
    			int bytesRead;
    			while ((bytesRead = response.Read(buf, 0, buf.Length)) > 0)
    			{
    				Console.WriteLine("Read {0} bytes", bytesRead);
    				for (int i = 0; i < bytesRead; ++i)
    					Console.Write ((char)buf[i]);
    				ResponseBody.Write(buf, 0, bytesRead);
    			}
            }

            ResponseBody.Position = 0;
		}

		public void Dispose()
		{
			ResponseBody.Dispose();
		}
	}
}
