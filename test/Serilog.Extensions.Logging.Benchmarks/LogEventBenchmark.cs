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
using Xunit;
using IMelLogger = Microsoft.Extensions.Logging.ILogger;

#pragma warning disable xUnit1013 // Public method should be marked as test

namespace Serilog.Extensions.Logging.Benchmarks
{
    [MemoryDiagnoser]
    public class LogEventBenchmark
    {
        private class Person
        {
            public string? Name { get; set; }
            public int Age { get; set; }
            public override string ToString() => "Fixed text";
        }

        readonly IMelLogger _melLogger;
        readonly Person _bob, _alice;
        readonly EventId _eventId = new EventId(1, "Test");

        public LogEventBenchmark()
        {
            var underlyingLogger = new LoggerConfiguration().CreateLogger();
            _melLogger = new SerilogLoggerProvider(underlyingLogger).CreateLogger(GetType().FullName!);
            _bob = new Person { Name = "Bob", Age = 42 };
            _alice = new Person { Name = "Alice", Age = 42 };
        }

        [Fact]
        public void Benchmark()
        {
            BenchmarkRunner.Run<LogEventBenchmark>();
        }

        [Benchmark]
        public void LogInformation()
        {
            _melLogger.LogInformation("Hi {@User} from {$Me}", _bob, _alice);
        }

        [Benchmark]
        public void LogInformationScoped()
        {
            using (var scope = _melLogger.BeginScope("Hi {@User} from {$Me}", _bob, _alice))
                _melLogger.LogInformation("Hi");
        }

        [Benchmark]
        public void LogInformation_WithEventId()
        {
            this._melLogger.Log(
                LogLevel.Information,
                _eventId,
                "Hi {@User} from {$Me}",
                _bob,
                _alice);
        }
    }
}
