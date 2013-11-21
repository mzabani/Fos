Fos (FastCgi Owin Server)
==========

This a small .NET 4/Mono compatible FastCgi Owin Server written in C#. This means that it handles FastCgi requests from a webserver such as Nginx or IIS, passes it to a Owin pipeline and responds back to the webserver. It depends on the [FastCgiNet](http://github.com/mzabani/FastCgiNet) library, another one of my GitHub repositories.

Usage
-----
This software is a library for self hosting. This means that you should add a reference to it in your Owin Web Application, and run it like this:

```
using Fos;
using Fos.Owin;
using Owin;

...
...

private static FosSelfHost FosServer;
public static void Main(string[] args)
{
	using (FosServer = new FosSelfHost(applicationRegistration))
	{
		// Bind on 127.0.0.1, port 9000
		fcgiSelfHost.Bind(System.Net.IPAddress.Loopback, 9000);

		// If you're on *nix and like unix sockets
		fcgiSelfHost.Bind("/tmp/fcgisocket.sock");

		// Start the server.
		fcgiSelfHost.Start(false);
	}
}

static void applicationRegistration(IAppBuilder builder)
{
	// To log statistics get access to the pretty statistics page, you need to create a shunt, like this:
	var statisticsPipeline = builder.New();
	// Here you would add statistics authentication middleware. One example would be only allowing connections from localhost or from admins
	// statisticsPipeline.Use<MyStatisticsPageAuthenticationMiddleware>();
	statisticsPipeline.UseStatisticsLogging(FosServer, new TimeSpan(0, 30, 0));

	// Will shunt to "statisticsPipeline" if request is to "/_stats"
	var statisticsMapping = new Dictionary<string, IAppBuilder>() { { "/_stats", statisticsPipeline } };
	builder.Use<ShuntMiddleware>(statisticsMapping); 

	// This is how you register your application's middleware. This is typically one of your Owin compatible Web frameworks
	builder.Use(typeof(MyApplicationType));
}
```

Building
--------
Currently there are no released versions. You have to clone this repository and [FastCgiNet](http://github.com/mzabani/FastCgiNet) as well, open both projects in a solution and update FastCgiServer's reference to FastCgiNet to the project in the solution. This solution can be opened in Monodevelop (this is the development IDE I use, in fact) or Visual Studio.

Error handling and logging
--------------------------
You can define a logger that is used internally when handling connections from the FastCgi Server and other operations. You just need to implement the Fos.Logging.IServerLogger interface and register your instance to your instance of FosSelfHost with the SetLogger method. Please note:
- Your implementation of IServerLogger MUST NOT THROW ANY EXCEPTIONS. Your implementation will work side by side with the server itself, and if throws an exception it _WILL_ crash the server. If you are unsure, make sure do add a `try .. catch` in every method of your implementation.
- If you have set a logger that implements IDisposable, it will be disposed when the server is disposed.
- If the application throws an exception, Fos *will* display it to the visitor. If you don't want exceptions showing, add middleware that will handle exceptions first thing in your pipeline.

You can also set a custom internal logger that ships with Fos. This logger logs access statistics, exceptions thrown by the application and serves a page that lists them in a nice/simple page. If this page doesn't suit you you're free to implement your own statistics logger, of course. This custom internal logger only ships for practical purposes. If you want to set it, look at the main example.

Current state and warning
-------------------------
Currently, this server seems to be compatible with NancyFx and Simple.Web (I'm not sure if it is compatible with other frameworks). To use these, you just need to call builder.UseNancy() or builder.UseSimpleWeb() (after using the appropriate namespaces, such as Nancy.Owin or Simple.Web.OwinSupport). Only very basic applications have been tested so far. That said, please do note:
- I've only used Fos with Nginx, on which it seems to run perfectly and handle a lot of requests. Also, Fos is *practically* guaranteed not to stop working if you don't risk yourself with unsafe code, so please give it a try!
- The API is *not* guaranteed to remain stable for a while, and most likely will not. Major changes are not expected, though, and since there is not a lot of room for breakage, API changes will probably take you only 5 minutes to adapt your code to.


To application builders
-----------------------
If you intend to build an Owin compatible application and run it with this server, be aware of the following:
- So far, the Owin CancellationToken found by the key "owin.CallCancelled" is signaled if and only if the FastCgi socket for the request has been closed

Non standard extensions:
- Fos' IAppBuilder implementation adds a CancellationToken to the Properties dictionary that is signaled when the server is stopped and/or disposed. You can reach this token by the key "host.OnAppDisposing"


Goals
-----
This project's goal is to provide a way for us to use what is best in all worlds to serve our web applications: A robust fastcgi compatible webserver of your choice (nginx, IIS, apache, lighttpd and many others), C# or any other language that compiles to CIL and any Mono compatible operating system.
It is also an attempt to stimulate the Owin and .NET OSS ecosystems, since ASP.NET on Mono is likely not to get too much attention in the future, and since it looks like a better, more open alternative to ASP.NET.
