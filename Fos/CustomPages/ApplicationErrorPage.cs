using System;

namespace Fos.CustomPages
{
	internal class ApplicationErrorPage : ICustomPage
	{
		private static string PageFormat = "<html><head><title>Application Error</title></head><body><h1>Application Error</h1><p>Your application could not process the request and threw the following exception:</p><p>{0}</p></body></html>";

		public string Contents { get; private set; }

		public ApplicationErrorPage(Exception e)
		{
			Contents = string.Format(PageFormat, e.ToString().Replace(System.Environment.NewLine, "<br />"));
		}
	}
}
