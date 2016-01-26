# Serilog.Extensions.Logging 
[![Build status](https://ci.appveyor.com/api/projects/status/865nohxfiq1rnby0/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-extensions-logging/branch/master) [![NuGet Version](http://img.shields.io/nuget/v/Serilog.Extensions.Logging.svg?style=flat)](https://www.nuget.org/packages/Serilog.Extensions.Logging/) 


A Serilog provider for [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging), the logging subsystem used by ASP.NET 5.

This package routes ASP.NET log messages through Serilog, so you can get information about ASP.NET's internal operations logged to the same Serilog sinks as your application events.

### Instructions

**First**, install the _Serilog.Extensions.Logging_ [NuGet package](https://www.nuget.org/packages/Serilog.Extensions.Logging) into your web or console app. You will need a way to view the log messages - _Serilog.Sinks.Literate_ writes these to the console.

```powershell
Install-Package Serilog.Extensions.Logging
Install-Package Serilog.Sinks.Literate
```

**Next**, in your application's `Startup` method, configure Serilog first:

```csharp
using Serilog;

public class Startup
{
  public Startup(IHostingEnvironment env)
  {
    Log.Logger = new LoggerConfiguration()
      .WriteTo.LiterateConsole()
      .CreateLogger();
      
    // Other startup code
```

**Finally**, in your `Startup` class's `Configure()` method, call `AddSerilog()` on the provided `loggerFactory`.

```csharp
  public void Configure(IApplicationBuilder app,
                        IHostingEnvironment env,
                        ILoggerFactory loggerfactory)
  {
      loggerfactory.AddSerilog();
```

That's it! With the level bumped up a little you should see log output like:

```
2015-05-15 22:14:44.646 +10:00 [DBG] RouteCollection.RouteAsync
	Routes: 
		Microsoft.AspNet.Mvc.Routing.AttributeRoute
		{controller=Home}/{action=Index}/{id?}
	Handled? True
2015-05-15 22:14:44.647 +10:00 [DBG] RouterMiddleware.Invoke
	Handled? True
2015-05-15 22:14:45.706 +10:00 [DBG] /lib/jquery/jquery.js not modified
2015-05-15 22:14:45.706 +10:00 [DBG] /css/site.css not modified
2015-05-15 22:14:45.741 +10:00 [DBG] Handled. Status code: 304 File: /css/site.css
```

### Levels

If you want to get more information from the log you'll need to bump up the level.

Two things:

 * You need to set `MinimumLevel` on **both** the Serilog `LoggerConfiguration` and the `ILoggerFactory`
 * Serilog and ASP.NET assign different priorities to the `Debug` and `Verbose` levels; Serilog's `Debug` is ASP.NET's `Verbose`, and vice-versa

### Building from source

To build the `dev` branch, which tracks the `dev` branch of _Microsoft.Extensions.Logging_, you must add https://www.myget.org/F/aspnetvnext/ to your package sources.

### Credits

This package evolved from an earlier package _Microsoft.Framework.Logging.Serilog_ [provided by the ASP.NET team](https://github.com/aspnet/Logging/pull/182).
