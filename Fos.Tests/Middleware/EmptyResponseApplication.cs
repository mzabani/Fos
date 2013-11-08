using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Fos.Owin;

namespace Fos.Tests.Middleware
{
	using OwinHandler = Func<IDictionary<string, object>, Task>;

	class EmptyResponseApplication
	{
		public Task Invoke(IDictionary<string, object> owinContext)
		{
			return Task.Factory.StartNew(() =>
            {
				var context = (OwinContext) owinContext;

				//context
			});
		}

		public EmptyResponseApplication (OwinHandler ignoreNext)
		{
		}
	}
}

