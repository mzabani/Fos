FastCgiServer
==========

This a small .NET 4/Mono compatible FastCgi Owin Server written in C#. This means that it handles FastCgi requests from a webserver such as Nginx, passes it to a Owin pipeline and responds back to the webserver. It depends on the FastCgiNet library, another one of my GitHub repositories.

Usage
-----
This software is a library for self hosting. This means that you should add a reference to it in your Owin Web Application, and run it like this:

```
using FastCgiServer;
using Owin;

public static void Main(string[] args)
{
	using (var fcgiSelfHost = new FCgiOwinSelfHost(applicationRegistration))
	{
		// Bind on 127.0.0.1, port 9000
		fcgiSelfHost.Bind(System.Net.IPAddress.Loopback, 9000);

		// Start the server. This method blocks
		fcgiSelfHost.Start();
	}
}

static void applicationRegistration(IAppBuilder builder)
{
	// This is how you register your application's middleware. This is typically one of your Owin compatible Web frameworks
	builder.Use(typeof(MyApplicationType));
}
```

More
----
This is very early documentation (as you can see) and a very early release. This project should not yet be used on production under any circumstances.

Goals
-----
This project's goal is to provide a way for us to use what is best in all worlds to server our web applications: A robust fastcgi compatible webserver of your choice (nginx, apache, lighttpd and many others), C# or any other language that compiles to CIL and any Mono compatible operating system.
It is also an attempt to stimulate the Owin ecosystem, since ASP.NET on Mono is likely not to get too much attention in the future, and since it looks like a better, more open alternative to ASP.NET.
