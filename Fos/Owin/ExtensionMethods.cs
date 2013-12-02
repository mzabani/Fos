using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Owin;

namespace Fos.Owin
{
	public static class ExtensionMethods
	{
		public static IAppBuilder Use<T>(this IAppBuilder builder, params object[] args)
		{
			return builder.Use(typeof(T), args);
		}

        /// <summary>
        /// Log statistics and show a perty page at <paramref name="relativePath"/>, accessible only if <paramref name="configureAuthentication"/> allows so.
        /// This method is just a way to <see cref="Use"/> an internal middleware that serves a page with the logged statistics.
        /// </summary>
        /// <param name="server">The instance of <see cref="Fos.FosSelfHost"/> that represents your server.</param> 
        /// <param name="aggregationInterval">The aggregation interval to show time aggregated statistics such as visit numbers. The logger will have lots of work to do if this is too small, so balance this carefully.</param>
        /// <remarks>You can use <see cref="Fos.Owin.ShuntMiddleware"/> to shunt requests to a certain path to a different <see cref="Owin.IAppBuilder"/> to serve the statistics page.</remarks>
        public static IAppBuilder UseStatisticsLogging(this IAppBuilder builder, FosSelfHost server, TimeSpan aggregationInterval)
        {
            if (builder is FosAppBuilder == false)
                throw new ArgumentException("The IAppBuilder must be the Fos's Application Builder implementation. Don't use this extension method with a different Owin Server implementation");
            else if (server == null)
                throw new ArgumentNullException("server");

            var fosBuilder = (FosAppBuilder)builder;
            var logger = new Fos.Logging.StatsLogger(aggregationInterval);
            server.StatisticsLogger = logger;
            fosBuilder.Use<Fos.Logging.StatsPageMiddleware>(logger);

            return fosBuilder;
        }

        public static string RequestPath(this IDictionary<string, object> context)
        {
            return (string)context["owin.RequestPathBase"] + (string)context["owin.RequestPath"];
        }
	}
}
