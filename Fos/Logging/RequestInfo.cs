using System;
using Fos.Listener;

namespace Fos.Logging
{
	/// <summary>
	/// Class that holds lots of request info, such as the requested Uri, the request method and others.
	/// </summary>
	public class RequestInfo
	{
		public string HttpMethod { get; private set; }
		public string RelativePath { get; private set; }
		public string QueryString { get; private set; }
		
		/// <summary>
		/// This defaults to 0 if no response was set by the application or if the connection was closed abruptly.
		/// </summary>
		public int ResponseStatusCode { get; private set; }
	
		internal RequestInfo(FosRequest req)
		{
			if (req == null)
				throw new ArgumentNullException("req");
				
			if (req.OwinContext == null)
				return;
			
			var ctx = req.OwinContext;
			
			// If the request was closed too quickly, we may not have received all parameters. Always check
			if (ctx.HttpMethodDefined)
				HttpMethod = ctx.HttpMethod;
			if (ctx.RelativePathDefined)
				RelativePath = ctx.RelativePath;
			
			QueryString = ctx.QueryString;
			
			if (ctx.SomeResponseExists)
				ResponseStatusCode = ctx.ResponseStatusCode;
		}
	}
}
