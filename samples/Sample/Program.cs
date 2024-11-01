using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

// Creating a `LoggerProviderCollection` lets Serilog optionally write
// events through other dynamically-added MEL ILoggerProviders.
var providers = new LoggerProviderCollection();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.Providers(providers)
    .CreateLogger();

var services = new ServiceCollection();

services.AddSingleton(providers);
services.AddSingleton<ILoggerFactory>(sc =>
{
    var providerCollection = sc.GetService<LoggerProviderCollection>();
    var factory = new SerilogLoggerFactory(null, true, providerCollection);

    foreach (var provider in sc.GetServices<ILoggerProvider>())
        factory.AddProvider(provider);

    return factory;
});

services.AddLogging(l => l.AddConsole());

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var eventId = new EventId(1001, "Test");

for (int i = 0; i < 1_000; i++)
{
    logger.Log(
        LogLevel.Information,
        eventId,
        "Subscription {SubscriptionId} for entity {EntityName} handler for message {MessageId} has been successfully completed.",
        "my-subscription-id",
        "TestQueue",
        1);
}
serviceProvider.Dispose();
