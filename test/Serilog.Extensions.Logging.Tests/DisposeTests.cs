using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Serilog.Extensions.Logging.Tests;

public class DisposeTests
{
    private readonly DisposableSink _sink;
    private readonly Logger _serilogLogger;

    public DisposeTests()
    {
        _sink = new DisposableSink();
        _serilogLogger = new LoggerConfiguration()
            .WriteTo.Sink(_sink)
            .CreateLogger();
    }

    [Fact]
    public void DisposesProviderWhenDisposeIsTrue()
    {
        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog(logger: _serilogLogger, dispose: true))
            .BuildServiceProvider();

        // Get a logger so that we ensure SerilogLoggerProvider is created
        var logger = services.GetRequiredService<ILogger<DisposeTests>>();
        logger.LogInformation("Hello, world!");

        services.Dispose();
        Assert.True(_sink.DisposeCalled);
        Assert.False(_sink.DisposeAsyncCalled);
    }

#if NET8_0_OR_GREATER
    [Fact]
    public async Task DisposesProviderAsyncWhenDisposeIsTrue()
    {
      var services = new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog(logger: _serilogLogger, dispose: true))
            .BuildServiceProvider();

        // Get a logger so that we ensure SerilogLoggerProvider is created
        var logger = services.GetRequiredService<ILogger<DisposeTests>>();
        logger.LogInformation("Hello, world!");

        await services.DisposeAsync();
        Assert.False(_sink.DisposeCalled);
        Assert.True(_sink.DisposeAsyncCalled);
    }
#endif

    private sealed class DisposableSink : ILogEventSink, IDisposable, IAsyncDisposable
    {
        public bool DisposeAsyncCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        public void Dispose() => DisposeCalled = true;
        public ValueTask DisposeAsync()
        {
            DisposeAsyncCalled = true;
            return default;
        }

        public void Emit(LogEvent logEvent)
        {
        }
    }
}
