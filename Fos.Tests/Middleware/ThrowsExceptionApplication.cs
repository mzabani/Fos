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
                // IF YOU WANT TO CHANGE THIS, WATCH OUT. A LOT OF CODE DEPENDS ON THIS
				throw new Exception("An error occured in the application. On purpose.");
			});
		}

		public ThrowsExceptionApplication(OwinHandler ignoreNext)
		{
		}
	}
}

