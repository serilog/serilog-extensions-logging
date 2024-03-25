using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;

// Configure a JsonFormatter to log out scope to the console
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();

// Setup Serilog with M.E.L, and configure the appropriate ActivityTrackingOptions
var services = new ServiceCollection();

services.AddLogging(l => l
    .AddSerilog()
    .Configure(options =>
    {
        options.ActivityTrackingOptions =
            ActivityTrackingOptions.SpanId
            | ActivityTrackingOptions.TraceId
            | ActivityTrackingOptions.ParentId
            | ActivityTrackingOptions.TraceState
            | ActivityTrackingOptions.TraceFlags
            | ActivityTrackingOptions.Tags
            | ActivityTrackingOptions.Baggage;
    }));

// Add an ActivityListener (required, otherwise Activities don't actually get created if nothing is listening to them)
ActivitySource.AddActivityListener(new ActivityListener
{
    ShouldListenTo = source => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded
});

// Run our test
var activitySource = new ActivitySource("SomeActivitySource");

using var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

using var activity = activitySource.StartActivity();

activity?.SetTag("tag.domain.id", 1234);
activity?.SetBaggage("baggage.environment", "uat");

using var scope = logger.BeginScope(new
{
    User = "Hugh Mann",
    Time = DateTimeOffset.UtcNow
});

logger.LogInformation("Hello world!");
