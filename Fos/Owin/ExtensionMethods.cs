using System;
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
	}
}
