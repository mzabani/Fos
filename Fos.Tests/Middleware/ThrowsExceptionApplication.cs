using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Fos.Owin;

namespace Fos.Tests.Middleware
{
	using OwinHandler = Func<IDictionary<string, object>, Task>;

	class ThrowsExceptionApplication
	{
		public Task Invoke(IDictionary<string, object> owinContext)
		{
			return Task.Factory.StartNew(() =>
            {
				throw new Exception("An error occured in the application. On purpose.");
			});
		}

		public ThrowsExceptionApplication(OwinHandler ignoreNext)
		{
		}
	}
}

