using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Owin;

namespace Fos.Owin
{
    using OwinHandler = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// Maps requests to certain paths (supplied as an IDictionary&lt;string, IAppBuilder&gt;) if the beginning of the request URL
    /// matches the string supplied in a folder-like way (e.g. /some/request/to/page will match an entry "/some" from the dictionary).
    /// Any request that does not match an entry is passed to the next handler in the pipeline.
    /// </summary>
    /// <remarks>
    /// Every string in the path mapping dictionary must start with a slash, this slash being the only one in the string.
    /// The context is never modified, whether a match is found or not.
    /// </remarks>
    public class ShuntMiddleware
    {
        private OwinHandler Next;
        private IDictionary<string, IAppBuilder> Paths;

        private readonly object buildLock = new object();
        private IDictionary<IAppBuilder, OwinHandler> ShuntsHandlers = new Dictionary<IAppBuilder, OwinHandler>();

        private IAppBuilder GetMatch(string requestPath)
        {
            string folderDir;
            int secondSlash = requestPath.IndexOf('/', 1);
            if (secondSlash > 0)
                folderDir = requestPath.Substring(0, secondSlash);
            else
                folderDir = requestPath;

            IAppBuilder builder;
            if (Paths.TryGetValue(folderDir, out builder))
                return builder;
            else
                return null;
        }

        public Task Invoke(IDictionary<string, object> context)
        {
            string requestPath = (string)context["owin.RequestPath"];
            var appBuilder = GetMatch(requestPath);

            if (appBuilder == null)
            {
                if (Next == null)
                    throw new EntryPointNotFoundException("No match found and no next handler defined"); //TODO: Is this exception appropriate?

                return Next(context);
            }

            // Builds the pipeline to be invoked and cache it
            OwinHandler handler;
            lock (buildLock)
            {
                if (!ShuntsHandlers.TryGetValue(appBuilder, out handler))
                {
                    handler = (OwinHandler)appBuilder.Build(typeof(OwinHandler));
                    ShuntsHandlers.Add(appBuilder, handler);
                }
            }

            return handler(context);
        }

        public ShuntMiddleware(OwinHandler next, IDictionary<string, IAppBuilder> buildersPaths)
        {
            Next = next;
            Paths = buildersPaths ?? new Dictionary<string, IAppBuilder>();
        }
    }
}
