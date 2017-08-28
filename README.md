# Serilog.Extensions.Logging [![Build status](https://ci.appveyor.com/api/projects/status/865nohxfiq1rnby0/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-framework-logging/history) [![NuGet Version](http://img.shields.io/nuget/v/Serilog.Extensions.Logging.svg?style=flat)](https://www.nuget.org/packages/Serilog.Extensions.Logging/) 


A Serilog provider for [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging), the logging subsystem used by ASP.NET Core.

### ASP.NET Core 2.0+ Instructions

ASP.NET Core 2.0 applications should prefer [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore) and `UseSerilog()` instead.

### ASP.NET Core 1.0, 1.1, and Default Provider Integration

The package implements `AddSerilog()` on `ILoggingBuilder` and `ILoggerFactory` to enable the Serilog provider under the default _Microsoft.Extensions.Logging_ implementation.

**First**, install the _Serilog.Extensions.Logging_ [NuGet package](https://www.nuget.org/packages/Serilog.Extensions.Logging) into your web or console app. You will need a way to view the log messages - _Serilog.Sinks.Console_ writes these to the console.

```powershell
Install-Package Serilog.Extensions.Logging -DependencyVersion Highest
Install-Package Serilog.Sinks.Console
```

**Next**, in your application's `Startup` method, configure Serilog first:

```csharp
using Serilog;

public class Startup
{
  public Startup(IHostingEnvironment env)
  {
    Log.Logger = new LoggerConfiguration()
      .Enrich.FromLogContext()
      .WriteTo.Console()
      .CreateLogger();
      
    // Other startup code
```

**Finally**, in your `Startup` class's `Configure()` method, remove the existing logger configuration entries and
call `AddSerilog()` on the provided `loggingBuilder`.

```csharp
  public void ConfigureServices(IServiceCollection services)
  {
      services.AddLogging(loggingBuilder =>
      	loggingBuilder.AddSerilog(dispose: true));
      
      // Other services ...
  }
```

That's it! With the level bumped up a little you should see log output like:

```
[22:14:44.646 DBG] RouteCollection.RouteAsync
	Routes: 
		Microsoft.AspNet.Mvc.Routing.AttributeRoute
		{controller=Home}/{action=Index}/{id?}
	Handled? True
[22:14:44.647 DBG] RouterMiddleware.Invoke
	Handled? True
[22:14:45.706 DBG] /lib/jquery/jquery.js not modified
[22:14:45.706 DBG] /css/site.css not modified
[22:14:45.741 DBG] Handled. Status code: 304 File: /css/site.css
```

### Credits

This package evolved from an earlier package _Microsoft.Framework.Logging.Serilog_ [provided by the ASP.NET team](https://github.com/aspnet/Logging/pull/182).
