using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace Fos.Owin
{
	/// <summary>
	/// This class is a stub that can be used to define a 404 error page. Register this before your application's middleware.
	/// </summary>
	public class PageNotFoundMiddleware
	{
		Func<IDictionary<string, object>, Task> Next;
		string PageNotFoundHtmlFormat = @"<html><head><title>Page not found</title></head><body><h1>Page not found</h1>The page at URL <i>{0}</i> was not found. Check your spelling.</body></html>";

		private Task Invoke(IDictionary<string, object> owinParameters)
		{
			// Invoke the next and act when the response is ready
			if (Next == null)
				throw new Exception("There is no next middleware in the pipeline. The PageNotFoundMiddleware can't work without a next middleware to invoke.");

			Task completionTask = Next(owinParameters);

			return completionTask.ContinueWith(t =>
			{
				var prms = (OwinContext) owinParameters;
				
				int httpStatusCode;
				try
				{
					httpStatusCode = (int)prms["owin.ResponseStatusCode"];
				}
				catch (KeyNotFoundException)
				{
					// Default status code
					httpStatusCode = 200;
				}
				
				if (httpStatusCode == 404)
				{
					// Set the headers
					prms.SetResponseHeader("Content-Type", "text/html");

					// Write our output.. what if the stream has already been written to?
					using (var writer = new StreamWriter(prms.ResponseBody))
					{
						writer.Write(string.Format(PageNotFoundHtmlFormat, prms.CompleteUri));
					}
				}
			});
		}

		private PageNotFoundMiddleware (Func<IDictionary<string, object>, Task> next)
		{
			Next = next;
		}
	}
}
