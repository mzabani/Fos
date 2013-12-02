using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Owin;

namespace Fos.Owin
{
	internal class FosAppBuilder : IAppBuilder
	{
		private Dictionary<string, object> properties;

		public CancellationToken OnAppDisposing { get; private set; }

		private FosOwinRoot RootMiddleware;

		/// <summary>
		/// This is the last middleware added through <see cref="Use"/>, or the <see cref="RootMiddleWare"/> in case <see cref="Use"/> has not been called yet.
		/// </summary>
		private OwinMiddleware LastMiddleware;

		public FosAppBuilder(CancellationToken cancelToken)
		{
			properties = new Dictionary<string, object>();
			RootMiddleware = new FosOwinRoot();

			//WARN: Non standard Owin header. Used by Nancy
			OnAppDisposing = cancelToken;
			properties.Add("host.OnAppDisposing", cancelToken);
		}

		public IAppBuilder Use(object middleware, params object[] args)
		{
			Delegate delegateMiddleware = middleware as Delegate;
			OwinMiddleware newMiddleware;
			if (delegateMiddleware != null)
			{
				newMiddleware = new OwinMiddleware(delegateMiddleware, args);
			}
			else
			{
				Type typeMiddleware = middleware as Type;

				if (typeMiddleware != null)
					newMiddleware = new OwinMiddleware(typeMiddleware, args);
				else
					throw new ArgumentException("The middleware to be used needs either to be a Type or a Delegate");
			}

			// Update the chain of middleware
			if (LastMiddleware == null)
				RootMiddleware.Next = newMiddleware;
			else
				LastMiddleware.Next = newMiddleware;

			LastMiddleware = newMiddleware;

			return this;
		}

		public object Build (Type returnType)
		{
			if (returnType == typeof(Func<IDictionary<string, object>, Task>))
			{
				return (Func<IDictionary<string, object>, Task>)RootMiddleware.Invoke;
			}
			else
				throw new NotSupportedException("Only Func<IDictionary<string, object>, Task> is supported right now");
		}

		public IAppBuilder New ()
		{
			return new FosAppBuilder(OnAppDisposing);
		}

		public IDictionary<string, object> Properties {
			get
            {
				return properties;
			}
		}
	}
}
