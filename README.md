# Serilog.Extensions.Logging [![Build status](https://ci.appveyor.com/api/projects/status/865nohxfiq1rnby0/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-framework-logging/history) [![NuGet Version](http://img.shields.io/nuget/v/Serilog.Extensions.Logging.svg?style=flat)](https://www.nuget.org/packages/Serilog.Extensions.Logging/)

A Serilog provider for [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging), the logging subsystem used by ASP.NET Core.

### ASP.NET Core Instructions

**ASP.NET Core** applications should prefer [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore) and `UseSerilog()` instead.

### Non-web .NET Core Instructions

**Non-web .NET Core** applications should prefer [Serilog.Extensions.Hosting](https://github.com/serilog/serilog-extensions-hosting) and `UseSerilog()` instead.

### .NET Core 1.0, 1.1 and Default Provider Integration

The package implements `AddSerilog()` on `ILoggingBuilder` and `ILoggerFactory` to enable the Serilog provider under the default _Microsoft.Extensions.Logging_ implementation.

**First**, install the _Serilog.Extensions.Logging_ [NuGet package](https://www.nuget.org/packages/Serilog.Extensions.Logging) into your web or console app. You will need a way to view the log messages - _Serilog.Sinks.Console_ writes these to the console.

```sh
dotnet add package Serilog.Extensions.Logging
dotnet add package Serilog.Sinks.Console
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

**Finally, for .NET Core 2.0+**, in your `Startup` class's `Configure()` method, remove the existing logger configuration entries and
call `AddSerilog()` on the provided `loggingBuilder`.

```csharp
  public void ConfigureServices(IServiceCollection services)
  {
      services.AddLogging(loggingBuilder =>
      	loggingBuilder.AddSerilog(dispose: true));

      // Other services ...
  }
```

**For .NET Core 1.0 or 1.1**, in your `Startup` class's `Configure()` method, remove the existing logger configuration entries and call `AddSerilog()` on the provided `loggerFactory`.

```
  public void Configure(IApplicationBuilder app,
                        IHostingEnvironment env,
                        ILoggerFactory loggerfactory,
                        IApplicationLifetime appLifetime)
  {
      loggerfactory.AddSerilog();

      // Ensure any buffered events are sent at shutdown
      appLifetime.ApplicationStopped.Register(Log.CloseAndFlush);
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

### Notes on Log Scopes

_Microsoft.Extensions.Logging_ provides the `BeginScope` API, which can be used to add arbitrary properties to log events within a certain region of code. The API comes in two forms:

1. The method: `IDisposable BeginScope<TState>(TState state)`
2. The extension method: `IDisposable BeginScope(this ILogger logger, string messageFormat, params object[] args)`

Using the extension method will add a `Scope` property to your log events. This is most useful for adding simple "scope strings" to your events, as in the following code:

```csharp
using (_logger.BeginScope("Transaction")) {
    _logger.LogInformation("Beginning...");
    _logger.LogInformation("Completed in {DurationMs}ms...", 30);
}
// Example JSON output:
// {"@t":"2020-10-29T19:05:56.4126822Z","@m":"Beginning...","@i":"f6a328e9","SourceContext":"SomeNamespace.SomeService","Scope":["Transaction"]}
// {"@t":"2020-10-29T19:05:56.4176816Z","@m":"Completed in 30ms...","@i":"51812baa","DurationMs":30,"SourceContext":"SomeNamespace.SomeService","Scope":["Transaction"]}
```

If you simply want to add a "bag" of additional properties to your log events, however, this extension method approach can be overly verbose. For example, to add `TransactionId` and `ResponseJson` properties to your log events, you would have to do something like the following:

```csharp
// WRONG! Prefer the dictionary approach below instead
using (_logger.BeginScope("TransactionId: {TransactionId}, ResponseJson: {ResponseJson}", 12345, jsonString)) {
    _logger.LogInformation("Completed in {DurationMs}ms...", 30);
}
// Example JSON output:
// {
//	"@t":"2020-10-29T19:05:56.4176816Z",
//	"@m":"Completed in 30ms...",
//	"@i":"51812baa",
//	"DurationMs":30,
//	"SourceContext":"SomeNamespace.SomeService",
//	"TransactionId": 12345,
//	"ResponseJson": "{ \"Key1\": \"Value1\", \"Key2\": \"Value2\" }",
//	"Scope":["TransactionId: 12345, ResponseJson: { \"Key1\": \"Value1\", \"Key2\": \"Value2\" }"]
// }
```

Not only does this add the unnecessary `Scope` property to your event, but it also duplicates serialized values between `Scope` and the intended properties, as you can see here with `ResponseJson`. If this were "real" JSON like an API response, then a potentially very large block of text would be duplicated within your log event!
Moreover, the template string within `BeginScope` is rather arbitrary when all you want to do is add a bag of properties, and you start mixing enriching concerns with formatting concerns.

A far better alternative is to use the `BeginScope<TState>(TState state)` method. If you provide any `IEnumerable<KeyValuePair<string, object>>` to this method, then Serilog will output the key/value pairs as structured properties _without_ the `Scope` property, as in this example:

```csharp
var scopeProps = new Dictionary<string, object> {
    { "TransactionId", 12345 },
    { "ResponseJson", jsonString },
};
using (_logger.BeginScope(scopeProps) {
    _logger.LogInformation("Transaction completed in {DurationMs}ms...", 30);
}
// Example JSON output:
// {
//	"@t":"2020-10-29T19:05:56.4176816Z",
//	"@m":"Completed in 30ms...",
//	"@i":"51812baa",
//	"DurationMs":30,
//	"SourceContext":"SomeNamespace.SomeService",
//	"TransactionId": 12345,
//	"ResponseJson": "{ \"Key1\": \"Value1\", \"Key2\": \"Value2\" }"
// }
```

### Versioning

This package tracks the versioning and target framework support of its [_Microsoft.Extensions.Logging_](https://nuget.org/packages/Microsoft.Extensions.Logging) dependency.

### Credits

This package evolved from an earlier package _Microsoft.Framework.Logging.Serilog_ [provided by the ASP.NET team](https://github.com/aspnet/Logging/pull/182).
