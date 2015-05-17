# Serilog.Framework.Logging

A serilog provider for [Microsoft.Framework.Logging](https://www.nuget.org/packages/Microsoft.Framework.Logging), the logging subsystem used by ASP.NET 5.

This package routes ASP.NET log messages through Serilog, so you can get information about ASP.NET's internal operations logged to the same Serilog sinks as your application events.

### Instructions

**First**, install the _Serilog.Framework.Logging_ [NuGet package](https://www.nuget.org/packages/Serilog.Framework.Logging) into your web or console app.

**Next**, in your application's `Startup` method, configure Serilog first:

```csharp
using Serilog;

public class Startup
{
  public Startup(IHostingEnvironment env)
  {
    Log.Logger = new LoggerConfiguration()
#if DNXCORE50
      .WriteTo.TextWriter(Console.Out)
#else
      .WriteTo.Trace()
#endif
      .CreateLogger();
      
    // Other startup code
```

The conditional compilation (`#if`) is necessary if you're targeting the CoreCLR runtime, for which there are currenlty few Serilog sinks. If you're targeting the full .NET framework you can just use `.WriteTo.Trace()`, or any other available sink.

**Finally**, in your `Startup` class's `Configure()` method, call `AddSerilog()` on the provided `loggerFactory`.

```csharp
  public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerfactory)
  {
      loggerfactory.AddSerilog();
```

That's it! With the level bumped up a little you should see log output like:

```
2015-05-15 22:14:44.646 +10:00 [Debug] RouteCollection.RouteAsync
	Routes: 
		Microsoft.AspNet.Mvc.Routing.AttributeRoute
		{controller=Home}/{action=Index}/{id?}
	Handled? True
2015-05-15 22:14:44.647 +10:00 [Debug] RouterMiddleware.Invoke
	Handled? True
2015-05-15 22:14:45.706 +10:00 [Debug] /lib/jquery/jquery.js not modified
2015-05-15 22:14:45.706 +10:00 [Debug] /css/site.css not modified
2015-05-15 22:14:45.741 +10:00 [Debug] Handled. Status code: 304 File: /css/site.css
```

### Levels

If you want to get more information from the log you'll need to bump up the level.

Two things:

 * You need to set `MinimumLevel` on **both** the Serilog `LoggerConfiguration` and the `ILoggerFactory`
 * Serilog and ASP.NET assign different priorities to the `Debug` and `Trace` levels; Serilog's `Debug` is ASP.NET's `Trace`, and vice-versa

### Building from source

To build the `dev` branch, which tracks the `dev` branch of _Microsoft.Framework.Logging_, you must add https://www.myget.org/F/aspnetvnext/ to your package sources.

### Credits

This package evolved from an earlier package _Microsoft.Framework.Logging.Serilog_ [provided by the ASP.NET team](https://github.com/aspnet/Logging/pull/182).
