This is a simple example illustrating the use of Serilog with a web app.

The project was created with the following steps.

* Create the project and add NuGet dependencies
```
dotnet new web
dotnet add package Serilog.Extensions.Logging
dotnet add package Serilog.Sinks.Literate
```

* Extend the logging to include Serilog.  See `Program.cs`
```
.ConfigureLogging(log =>
{
    log.AddSerilog(logger: Log.Logger, dispose: true);
})
```

* Logging can then used directly to Serilog or via the `Microsoft.Extensions.Logging` pipeline.