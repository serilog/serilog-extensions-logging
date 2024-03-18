// Copyright 2019 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using IMelLogger = Microsoft.Extensions.Logging.ILogger;
using Serilog.Events;
using Serilog.Extensions.Logging.Benchmarks.Support;
using Xunit;

namespace Serilog.Extensions.Logging.Benchmarks;

[MemoryDiagnoser]
public class EventIdCapturingBenchmark
{
    readonly IMelLogger _melLogger;
    readonly ILogger _serilogContextualLogger;
    readonly CapturingSink _sink;
    const int LowId = 10, HighId = 101;
    const string Template = "This is an event";

    public EventIdCapturingBenchmark()
    {
        _sink = new CapturingSink();
        var underlyingLogger = new LoggerConfiguration().WriteTo.Sink(_sink).CreateLogger();
        _serilogContextualLogger = underlyingLogger.ForContext<EventIdCapturingBenchmark>();
        _melLogger = new SerilogLoggerProvider(underlyingLogger).CreateLogger(GetType().FullName!);
    }

    static void VerifyEventId(LogEvent? evt, int? expectedId)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        if (expectedId == null)
        {
            Assert.False(evt.Properties.TryGetValue("EventId", out _));
        }
        else
        {
            Assert.True(evt.Properties.TryGetValue("EventId", out var eventIdValue));
            var structure = Assert.IsType<StructureValue>(eventIdValue);
            var idValue = Assert.Single(structure.Properties, p => p.Name == "Id")?.Value;
            var scalar = Assert.IsType<ScalarValue>(idValue);
            Assert.Equal(expectedId.Value, scalar.Value);
        }
    }

    [Fact]
    public void Verify()
    {
        VerifyEventId(Native(), null);
        VerifyEventId(NoId(), null);
        VerifyEventId(LowNumbered(), LowId);
        VerifyEventId(HighNumbered(), HighId);
    }

    [Fact]
    public void Benchmark()
    {
        BenchmarkRunner.Run<EventIdCapturingBenchmark>();
    }

    [Benchmark(Baseline = true)]
    public LogEvent? Native()
    {
        _serilogContextualLogger.Information(Template);
        return _sink.Collect();
    }

    [Benchmark]
    public LogEvent? NoId()
    {
        _melLogger.LogInformation(Template);
        return _sink.Collect();
    }

    [Benchmark]
    public LogEvent? LowNumbered()
    {
        _melLogger.LogInformation(LowId, Template);
        return _sink.Collect();
    }

    [Benchmark]
    public LogEvent? HighNumbered()
    {
        _melLogger.LogInformation(HighId, Template);
        return _sink.Collect();
    }
}
