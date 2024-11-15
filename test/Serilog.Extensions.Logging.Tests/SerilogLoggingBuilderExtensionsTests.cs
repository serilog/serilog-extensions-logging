using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging.Tests.Support;
using Xunit;

namespace Serilog.Extensions.Logging.Tests;

public class SerilogLoggingBuilderExtensionsTests
{
    [Fact]
    public void AddSerilog_must_register_a_ILoggerProvider()
    {
        var services = new ServiceCollection()
            .AddLogging(builder => { builder.AddSerilog(); })
            .BuildServiceProvider();

        var loggerProviders = services.GetServices<ILoggerProvider>();
        Assert.Contains(loggerProviders, provider => provider is SerilogLoggerProvider);
    }

    [Fact]
    public void AddSerilog_must_register_a_ILoggerProvider_that_forwards_logs_to_static_Serilog_Logger()
    {
        var sink = new SerilogSink();
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var services = new ServiceCollection()
            .AddLogging(builder => { builder.AddSerilog(); })
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<SerilogLoggingBuilderExtensionsTests>>();
        logger.LogInformation("Hello, world!");

        Assert.Single(sink.Writes);
    }

    [Fact]
    public void AddSerilog_must_register_a_ILoggerProvider_that_forwards_logs_to_provided_logger()
    {
        var sink = new SerilogSink();
        var serilogLogger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var services = new ServiceCollection()
            .AddLogging(builder => { builder.AddSerilog(logger: serilogLogger); })
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<SerilogLoggingBuilderExtensionsTests>>();
        logger.LogInformation("Hello, world!");

        Assert.Single(sink.Writes);
    }
}
