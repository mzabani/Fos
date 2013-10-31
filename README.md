Fos (FastCgi Owin Server)
==========

This a small .NET 4/Mono compatible FastCgi Owin Server written in C#. This means that it handles FastCgi requests from a webserver such as Nginx, passes it to a Owin pipeline and responds back to the webserver. It depends on the [FastCgiNet](http://github.com/mzabani/FastCgiNet) library, another one of my GitHub repositories.

Usage
-----
This software is a library for self hosting. This means that you should add a reference to it in your Owin Web Application, and run it like this:

```
using Fos;
using Owin;

public static void Main(string[] args)
{
	using (var fcgiSelfHost = new FosOwinSelfHost(applicationRegistration))
	{
		// Bind on 127.0.0.1, port 9000
		fcgiSelfHost.Bind(System.Net.IPAddress.Loopback, 9000);

		// Start the server.
		fcgiSelfHost.Start(false);
	}
}

static void applicationRegistration(IAppBuilder builder)
{
	// This is how you register your application's middleware. This is typically one of your Owin compatible Web frameworks
	builder.Use(typeof(MyApplicationType));
}
```

Building
--------
Currently there are no released versions. You have to clone this repository and [FastCgiNet](http://github.com/mzabani/FastCgiNet) as well, open both projects in a solution and update FastCgiServer's reference to FastCgiNet to the project in the solution. This solution can be opened in Monodevelop (this is the development IDE I use, in fact) or Visual Studio.

Error handling and logging
--------------------------
You can define a logger that is used internally when handling connections from the FastCgi Server and other operations. You just need to implement the FastCgiNet.Logging.ILogger interface and register your instance to your instance of FosOwinSelfHost with the SetLogger method. At this point, a lot of Debug information is logged, since this project is in its infancy. Please note:
- If you have set a logger that implements IDisposable, it will be disposed when the server is disposed.
- If the application throws an exception, Fos *will* display it to the visitor. If you don't want exceptions showing, add middleware that will handle exceptions first thing in your pipeline.


Current state and warning
-------------------------
Currently, this server seems to be compatible with NancyFx and Simple.Web (I'm not sure if it is compatible with other frameworks). To use these, you just need to call builder.UseNancy() or builder.UseSimpleWeb() (after using the appropriate namespaces, such as Nancy.Owin or Simple.Web.OwinSupport). Only very basic applications have been tested so far. That said, please do note:
- I've only used Fos with Nginx, on which it seems to run perfectly and handle a lot of requests. Also, Fos is *practically* guaranteed not to stop working if you don't risk yourself with unsafe code, so please give it a try!
- The API is *not* guaranteed to remain stable for a while, and most likely will not. Major changes are not expected, though, and since there is not a lot of room for breakage, API changes will probably take you only 5 minutes to adapt your code to.


To application builders
-----------------------
If you intend to build an Owin compatible application and run it with this server, be aware of the following:
- So far, the CancellationToken is signaled if and only if the FastCgi socket for the request has been closed

Non standard extensions:
- Fos' IAppBuilder implementation adds a CancellationToken to the Properties dictionary that is signaled when the server is stopped and/or disposed. You can reach this token by the key "host.OnAppDisposing"


More
----
This is very early documentation (as you can see) and a very early release. This project should not yet be used on production under any circumstances.


Goals
-----
This project's goal is to provide a way for us to use what is best in all worlds to serve our web applications: A robust fastcgi compatible webserver of your choice (nginx, apache, lighttpd and many others), C# or any other language that compiles to CIL and any Mono compatible operating system.
It is also an attempt to stimulate the Owin and .NET OSS ecosystems, since ASP.NET on Mono is likely not to get too much attention in the future, and since it looks like a better, more open alternative to ASP.NET.
