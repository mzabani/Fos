using System;
using System.IO;
using System.Collections.Generic;

namespace Fos.Tests
{
	class BrowserResponse : IDisposable
	{
		public int StatusCode { get; private set; }
		public string StatusReason { get; private set; }
		public IDictionary<string, string> Headers { get; private set; }
		public Stream ResponseBody { get; private set; }

		public BrowserResponse(Stream response)
		{
			if (response == null)
				throw new ArgumentNullException ("response");

			int b;
			while ((b = response.ReadByte()) > 0)
				Console.Write ((char)b);

			response.Position = 0;

			Console.WriteLine ("1");
			ResponseBody = new MemoryStream();

			using (var reader = new StreamReader(response, System.Text.ASCIIEncoding.ASCII))
			{
				Console.WriteLine ("2");
				// First line is status code.. something quick and dirty will do
				string line = reader.ReadLine();
				int spaceIdx = line.IndexOf(' ');
				StatusCode = int.Parse(line.Substring(spaceIdx + 1, 3));
				StatusReason = line.Substring(spaceIdx + 4);
				Console.WriteLine ("3");

				// Reads the headers
				Headers = new Dictionary<string, string>();
				while ((line = reader.ReadLine()) != string.Empty)
				{
					spaceIdx = line.IndexOf(' ');
					string headerName = line.Substring(0, spaceIdx - 1);
					string headerValue = line.Substring(spaceIdx + 1);
					Console.WriteLine("{0}: {1}", headerName, headerValue);
					Headers.Add(headerName, headerValue);
				}
			}

			Console.WriteLine ("4");

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

			ResponseBody.Position = 0;
			response.Dispose();
		}

		public void Dispose()
		{
			ResponseBody.Dispose();
		}
	}
}
