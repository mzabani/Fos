using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Fos.Owin;

namespace Fos.Tests.Middleware
{
	using OwinHandler = Func<IDictionary<string, object>, Task>;

	class HelloWorldApplication
	{
		public Task Invoke(IDictionary<string, object> owinContext)
		{
			return Task.Factory.StartNew(() =>
            {
				var ctx = (OwinContext) owinContext;

				ctx.SetResponseHeader("Content-Type", "text/plain");
				using (var writer = new StreamWriter(ctx.ResponseBody))
				{
					writer.WriteLine("Hello World");
				}
			});
		}

		public HelloWorldApplication (OwinHandler ignoreNext)
		{
		}
	}
}

