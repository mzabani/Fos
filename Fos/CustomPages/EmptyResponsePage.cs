using System;

namespace Fos.CustomPages
{
	internal class EmptyResponsePage : ICustomPage
	{
		private static string StaticContent = "<html><head><title>Application Error</title></head><body><h1>Application Error</h1><p>The application did not set any headers or write a response</p></body></html>";

		public string Contents
		{
			get
			{
				return StaticContent;
			}
		}

		public EmptyResponsePage ()
		{
		}
	}
}

