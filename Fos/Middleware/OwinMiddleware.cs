using System;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fos.Owin
{
	internal class OwinMiddleware
	{
		/// <summary>
		/// The type of this middleware. An instance of this type will be created and Invoke() will be called on it if this middleware
		/// is not a simple Delegate type.
		/// </summary>
		Type MiddlewareType ;

		/// <summary>
		/// If this middleware is a simple Delegate type, then this field contains it.
		/// </summary>
		Delegate UntypedHandler;

		/// <summary>
		/// The args to be passed to this middleware, if any.
		/// </summary>
		object[] Args;

		/// <summary>
		/// Set this to define the next middleware to be called after the invocation of this middleware. If there is no next middleware, this is null.
		/// </summary>
		public OwinMiddleware Next;

		void BuildHandler()
		{
			// Is it a delegate or a type middleware?
			if (MiddlewareType != null)
			{
				// Instantiate the constructor with most arguments, limited by the amount of arguments we have available
				ConstructorInfo ctor;
				try
				{
					// filtered by matching all with Args type(except null Args)
					ctor = MiddlewareType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(c => c.GetParameters().Length >= 1 && c.GetParameters().Length - 1 <= Args.Length).OrderByDescending(c => c.GetParameters().Length)
						.Where((ct)=>{
							return ct.GetParameters().Zip(Args,(pinfo,arg)=>(arg==null)||(pinfo.ParameterType == arg.GetType()))
								.All(b=>b);
						}).First();
				}
				catch
				{
					throw new Exception("Couldn't find a constructor that takes at least one argument in type " + MiddlewareType);
				}
				
				object[] ctorArgs = new object[ctor.GetParameters().Length];
				ctorArgs[0] = Next == null ? null : Next.Handler;
				Array.Copy(Args, 0, ctorArgs, 1, ctorArgs.Length - 1);
				
				var obj = ctor.Invoke(ctorArgs);
				
				// Now call the Invoke method, which has to be Invoke(IDict..)
				//TODO: Check for perfect matching Invoke method signature
				var invokeMethod = MiddlewareType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).FirstOrDefault(m => m.Name == "Invoke" && m.ReturnType == typeof(Task));
				if (invokeMethod == null)
					throw new Exception("The next middleware does not have an appropriate Invoke method");

				Delegate d = Delegate.CreateDelegate(typeof(Func<IDictionary<string, object>, Task>), obj, "Invoke");
				_BuiltHandler = (Func<IDictionary<string, object>, Task>) d;
			}
			else
			{
				//TODO: What kind of delegate should we accept? This code begs improvement

				// NancyFx uses the delegate below
				var typedHandler1 = UntypedHandler as Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>;

				if (typedHandler1 != null)
				{
					_BuiltHandler = ((IDictionary<string, object> owinParams) => {
						var func = typedHandler1(Next == null ? null : Next.Handler);
						
						return func(owinParams);
					});

					return;
				}

				// Simple.Web uses this type of delegate
				var typedHandler2 = UntypedHandler as Func<IDictionary<string, object>, Func<IDictionary<string, object>, Task>, Task>;
				if (typedHandler2 != null)
				{
					_BuiltHandler = ((IDictionary<string, object> owinParams) => {
						var returnTask = typedHandler2(owinParams, Next == null ? null : Next.Handler);

						return returnTask;
					});

					return;
				}


				throw new NotSupportedException("Delegate type " + UntypedHandler + " is not supported");
			}
		}

		Func<IDictionary<string, object>, Task> _BuiltHandler;
		Func<IDictionary<string, object>, Task> Handler
		{
			get
			{
				if (_BuiltHandler != null)
					return _BuiltHandler;

				BuildHandler();

				return _BuiltHandler;
			}
		}

		public Task Invoke(IDictionary<string, object> owinParameters)
		{
			return Handler(owinParameters);
		}

		private OwinMiddleware(params object[] args)
		{
			if (args != null)
				Args = args;
			else
				Args = new object[0];
		}

		public OwinMiddleware (Type t, params object[] args)
			: this(args)
		{
			MiddlewareType = t;
		}

		public OwinMiddleware (Delegate handler, params object[] args)
			: this(args)
		{
			this.UntypedHandler = handler;
		}
	}
}

