using System;

namespace Fos.CustomPages
{
	internal interface ICustomPage
	{
		/// <summary>
		/// The entire HTML contents of the page.
		/// </summary>
		string Contents { get; }
	}
}

