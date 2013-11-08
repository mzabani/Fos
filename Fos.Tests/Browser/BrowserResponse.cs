using System;
using System.IO;
using System.Collections.Generic;

namespace Fos.Tests
{
	class BrowserResponse
	{
		public int StatusCode { get; private set; }
		public IDictionary<string, string> Headers { get; private set; }
		public Stream ResponseBody { get; private set; }

		public BrowserResponse(Stream response)
		{
			ResponseBody = new MemoryStream();

			using (var reader = new StreamReader(response))
			{
				// First line is status code.. something quick and dirty will do
				string firstLine = reader.ReadLine();
				Console.WriteLine(firstLine);
				int firstSpaceIdx = firstLine.IndexOf(' ');
				StatusCode = int.Parse(firstLine.Substring(firstSpaceIdx + 1, 3));

				//TODO: Headers

				// After reading an empty line, everything else is response body!
				while (reader.ReadLine() != "\r\n")
					;
			}

			// Write the response body
			byte[] buf = new byte[4096];
			int bytesRead;
			while ((bytesRead = response.Read(buf, 0, buf.Length)) > 0)
				ResponseBody.Write(buf, 0, bytesRead);
		}
	}
}
