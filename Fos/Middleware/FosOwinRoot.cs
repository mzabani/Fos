using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fos.Owin
{
	internal class FosOwinRoot
	{
		public OwinMiddleware Next;

		public Task Invoke(IDictionary<string, object> owinParameters)
		{
			// Do nothing yet, just pass control to next middleware

			if (Next != null)
				return Next.Invoke(owinParameters);

			throw new Exception("No middleware added to your app");
		}
	}
}
