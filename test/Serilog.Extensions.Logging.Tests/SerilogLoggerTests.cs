// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using Serilog.Events;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Debugging;
using Serilog.Extensions.Logging.Tests.Support;
using Xunit;

namespace Serilog.Extensions.Logging.Tests
{
    public class SerilogLoggerTest
    {
        const string Name = "test";
        const string TestMessage = "This is a test";

        static Tuple<SerilogLogger, SerilogSink> SetUp(LogLevel logLevel)
        {
            var sink = new SerilogSink();

            var serilogLogger = new LoggerConfiguration()
                .WriteTo.Sink(sink)
                .MinimumLevel.Is(LevelConvert.ToSerilogLevel(logLevel))
                .CreateLogger();

            var provider = new SerilogLoggerProvider(serilogLogger);
            var logger = (SerilogLogger)provider.CreateLogger(Name);

            return new Tuple<SerilogLogger, SerilogSink>(logger, sink);
        }

        [Fact]
        public void LogsWhenNullFilterGiven()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            logger.Log(LogLevel.Information, 0, TestMessage, null, null);

            Assert.Equal(1, sink.Writes.Count);
        }

        [Fact]
        public void LogsCorrectLevel()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            logger.Log(LogLevel.Trace, 0, TestMessage, null, null);
            logger.Log(LogLevel.Debug, 0, TestMessage, null, null);
            logger.Log(LogLevel.Information, 0, TestMessage, null, null);
            logger.Log(LogLevel.Warning, 0, TestMessage, null, null);
            logger.Log(LogLevel.Error, 0, TestMessage, null, null);
            logger.Log(LogLevel.Critical, 0, TestMessage, null, null);

            Assert.Equal(6, sink.Writes.Count);
            Assert.Equal(LogEventLevel.Verbose, sink.Writes[0].Level);
            Assert.Equal(LogEventLevel.Debug, sink.Writes[1].Level);
            Assert.Equal(LogEventLevel.Information, sink.Writes[2].Level);
            Assert.Equal(LogEventLevel.Warning, sink.Writes[3].Level);
            Assert.Equal(LogEventLevel.Error, sink.Writes[4].Level);
            Assert.Equal(LogEventLevel.Fatal, sink.Writes[5].Level);
        }

        [Theory]
        [InlineData(LogLevel.Trace, LogLevel.Trace, 1)]
        [InlineData(LogLevel.Trace, LogLevel.Debug, 1)]
        [InlineData(LogLevel.Trace, LogLevel.Information, 1)]
        [InlineData(LogLevel.Trace, LogLevel.Warning, 1)]
        [InlineData(LogLevel.Trace, LogLevel.Error, 1)]
        [InlineData(LogLevel.Trace, LogLevel.Critical, 1)]
        [InlineData(LogLevel.Debug, LogLevel.Trace, 0)]
        [InlineData(LogLevel.Debug, LogLevel.Debug, 1)]
        [InlineData(LogLevel.Debug, LogLevel.Information, 1)]
        [InlineData(LogLevel.Debug, LogLevel.Warning, 1)]
        [InlineData(LogLevel.Debug, LogLevel.Error, 1)]
        [InlineData(LogLevel.Debug, LogLevel.Critical, 1)]
        [InlineData(LogLevel.Information, LogLevel.Trace, 0)]
        [InlineData(LogLevel.Information, LogLevel.Debug, 0)]
        [InlineData(LogLevel.Information, LogLevel.Information, 1)]
        [InlineData(LogLevel.Information, LogLevel.Warning, 1)]
        [InlineData(LogLevel.Information, LogLevel.Error, 1)]
        [InlineData(LogLevel.Information, LogLevel.Critical, 1)]
        [InlineData(LogLevel.Warning, LogLevel.Trace, 0)]
        [InlineData(LogLevel.Warning, LogLevel.Debug, 0)]
        [InlineData(LogLevel.Warning, LogLevel.Information, 0)]
        [InlineData(LogLevel.Warning, LogLevel.Warning, 1)]
        [InlineData(LogLevel.Warning, LogLevel.Error, 1)]
        [InlineData(LogLevel.Warning, LogLevel.Critical, 1)]
        [InlineData(LogLevel.Error, LogLevel.Trace, 0)]
        [InlineData(LogLevel.Error, LogLevel.Debug, 0)]
        [InlineData(LogLevel.Error, LogLevel.Information, 0)]
        [InlineData(LogLevel.Error, LogLevel.Warning, 0)]
        [InlineData(LogLevel.Error, LogLevel.Error, 1)]
        [InlineData(LogLevel.Error, LogLevel.Critical, 1)]
        [InlineData(LogLevel.Critical, LogLevel.Trace, 0)]
        [InlineData(LogLevel.Critical, LogLevel.Debug, 0)]
        [InlineData(LogLevel.Critical, LogLevel.Information, 0)]
        [InlineData(LogLevel.Critical, LogLevel.Warning, 0)]
        [InlineData(LogLevel.Critical, LogLevel.Error, 0)]
        [InlineData(LogLevel.Critical, LogLevel.Critical, 1)]
        public void LogsWhenEnabled(LogLevel minLevel, LogLevel logLevel, int expected)
        {
            var (logger, sink) = SetUp(minLevel);

            logger.Log(logLevel, 0, TestMessage, null, null);

            Assert.Equal(expected, sink.Writes.Count);
        }

        [Fact]
        public void LogsCorrectMessage()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            logger.Log<object>(LogLevel.Information, 0, null, null, null);
            logger.Log(LogLevel.Information, 0, TestMessage, null, null);
            logger.Log<object>(LogLevel.Information, 0, null, null, (_, __) => TestMessage);

            Assert.Equal(3, sink.Writes.Count);

            Assert.Equal(1, sink.Writes[0].Properties.Count);
            Assert.Empty(sink.Writes[0].RenderMessage());

            Assert.Equal(2, sink.Writes[1].Properties.Count);
            Assert.True(sink.Writes[1].Properties.ContainsKey("State"));
            Assert.Equal(TestMessage, sink.Writes[1].RenderMessage());

            Assert.Equal(2, sink.Writes[2].Properties.Count);
            Assert.True(sink.Writes[2].Properties.ContainsKey("Message"));
            Assert.Equal(TestMessage, sink.Writes[2].RenderMessage());
        }

        [Fact]
        public void CarriesException()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            var exception = new Exception();

            logger.Log(LogLevel.Information, 0, "Test", exception, null);

            Assert.Equal(1, sink.Writes.Count);
            Assert.Same(exception, sink.Writes[0].Exception);
        }

        [Fact]
        public void SingleScopeProperty()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            using (logger.BeginScope(new FoodScope("pizza")))
            {
                logger.Log(LogLevel.Information, 0, TestMessage, null, null);
            }

            Assert.Equal(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("Name"));
            Assert.Equal("\"pizza\"", sink.Writes[0].Properties["Name"].ToString());
        }

        [Fact]
        public void StringifyScopeProperty()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            using (logger.BeginScope("{$values}", new [] { 1, 2, 3, 4 }))
            {
                logger.Log(LogLevel.Information, 0, TestMessage, null, null);
            }

            Assert.Equal(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("values"));
            Assert.Equal("\"System.Int32[]\"", sink.Writes[0].Properties["values"].ToString());
        }

        [Fact]
        public void NestedScopeSameProperty()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            using (logger.BeginScope(new FoodScope("avocado")))
            {
                using (logger.BeginScope(new FoodScope("bacon")))
                {
                    logger.Log(LogLevel.Information, 0, TestMessage, null, null);
                }
            }

            // Should retain the property of the most specific scope
            Assert.Equal(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("Name"));
            Assert.Equal("\"bacon\"", sink.Writes[0].Properties["Name"].ToString());
        }

        [Fact]
        public void NestedScopesDifferentProperties()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            using (logger.BeginScope(new FoodScope("spaghetti")))
            {
                using (logger.BeginScope(new LuckyScope(7)))
                {
                    logger.Log(LogLevel.Information, 0, TestMessage, null, null);
                }
            }

            Assert.Equal(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("Name"));
            Assert.Equal("\"spaghetti\"", sink.Writes[0].Properties["Name"].ToString());
            Assert.True(sink.Writes[0].Properties.ContainsKey("LuckyNumber"));
            Assert.Equal("7", sink.Writes[0].Properties["LuckyNumber"].ToString());
        }

        [Fact]
        public void CarriesMessageTemplateProperties()
        {
            var selfLog = new StringWriter();
            SelfLog.Enable(selfLog);

            var (logger, sink) = SetUp(LogLevel.Trace);

            logger.LogInformation("Hello, {Recipient}", "World");

            Assert.True(sink.Writes[0].Properties.ContainsKey("Recipient"));
            Assert.Equal("\"World\"", sink.Writes[0].Properties["Recipient"].ToString());
            Assert.Equal("Hello, {Recipient}", sink.Writes[0].MessageTemplate.Text);

            SelfLog.Disable();
            Assert.Empty(selfLog.ToString());
        }

        [Fact]
        public void CarriesMessageTemplatePropertiesWhenStringificationIsUsed()
        {
            var selfLog = new StringWriter();
            SelfLog.Enable(selfLog);
            var (logger, sink) = SetUp(LogLevel.Trace);
            var array = new[] { 1, 2, 3, 4 };

            logger.LogInformation("{$array}", array);

            Assert.True(sink.Writes[0].Properties.ContainsKey("array"));
            Assert.Equal("\"System.Int32[]\"", sink.Writes[0].Properties["array"].ToString());
            Assert.Equal("{$array}", sink.Writes[0].MessageTemplate.Text);

            SelfLog.Disable();
            Assert.Empty(selfLog.ToString());
        }

        [Fact]
        public void CarriesEventIdIfNonzero()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            const int expected = 42;

            logger.Log(LogLevel.Information, expected, "Test", null, null);

            Assert.Equal(1, sink.Writes.Count);

            var eventId = (StructureValue) sink.Writes[0].Properties["EventId"];
            var id = (ScalarValue) eventId.Properties.Single(p => p.Name == "Id").Value;
            Assert.Equal(42, id.Value);
        }

        [Fact]
        public void WhenDisposeIsFalseProvidedLoggerIsNotDisposed()
        {
            var logger = new DisposeTrackingLogger();
            // ReSharper disable once RedundantArgumentDefaultValue
            var provider = new SerilogLoggerProvider(logger, false);
            provider.Dispose();
            Assert.False(logger.IsDisposed);
        }

        [Fact]
        public void WhenDisposeIsTrueProvidedLoggerIsDisposed()
        {
            var logger = new DisposeTrackingLogger();
            var provider = new SerilogLoggerProvider(logger, true);
            provider.Dispose();
            Assert.True(logger.IsDisposed);
        }

        [Fact]
        public void BeginScopeDestructuresObjectsWhenDestructurerIsUsedInMessageTemplate()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            using (logger.BeginScope("{@Person}", new Person { FirstName = "John", LastName = "Smith" }))
            {
                logger.Log(LogLevel.Information, 0, TestMessage, null, null);
            }

            Assert.Equal(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("Person"));

            var person = (StructureValue)sink.Writes[0].Properties["Person"];
            var firstName = (ScalarValue)person.Properties.Single(p => p.Name == "FirstName").Value;
            var lastName = (ScalarValue)person.Properties.Single(p => p.Name == "LastName").Value;
            Assert.Equal("John", firstName.Value);
            Assert.Equal("Smith", lastName.Value);
        }

        [Fact]
        public void BeginScopeDestructuresObjectsWhenDestructurerIsUsedInDictionary()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);
            
            using (logger.BeginScope(new Dictionary<string, object> {{ "@Person", new Person { FirstName = "John", LastName = "Smith" }}}))
            {
                logger.Log(LogLevel.Information, 0, TestMessage, null, null);
            }

            Assert.Equal(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("Person"));

            var person = (StructureValue)sink.Writes[0].Properties["Person"];
            var firstName = (ScalarValue)person.Properties.Single(p => p.Name == "FirstName").Value;
            var lastName = (ScalarValue)person.Properties.Single(p => p.Name == "LastName").Value;
            Assert.Equal("John", firstName.Value);
            Assert.Equal("Smith", lastName.Value);
        }

        [Fact]
        public void BeginScopeDoesNotModifyKeyWhenDestructurerIsNotUsedInMessageTemplate()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            using (logger.BeginScope("{FirstName}", "John"))
            {
                logger.Log(LogLevel.Information, 0, TestMessage, null, null);
            }

            Assert.Equal(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("FirstName"));
        }

        [Fact]
        public void BeginScopeDoesNotModifyKeyWhenDestructurerIsNotUsedInDictionary()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            using (logger.BeginScope(new Dictionary<string, object> { { "FirstName", "John"}}))
            {
                logger.Log(LogLevel.Information, 0, TestMessage, null, null);
            }

            Assert.Equal(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("FirstName"));
        }

        [Fact]
        public void NamedScopesAreCaptured()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            using (logger.BeginScope("Outer"))
            using (logger.BeginScope("Inner"))
            {
                logger.Log(LogLevel.Information, 0, TestMessage, null, null);
            }

            Assert.Equal(1, sink.Writes.Count);

            Assert.True(sink.Writes[0].Properties.TryGetValue(SerilogLoggerProvider.ScopePropertyName, out var scopeValue));
            var sequence = Assert.IsType<SequenceValue>(scopeValue);
            var items = sequence.Elements.Select(e => Assert.IsType<ScalarValue>(e).Value).Cast<string>().ToArray();
            Assert.Equal(2, items.Length);
            Assert.Equal("Outer", items[0]);
            Assert.Equal("Inner", items[1]);
        }

        class FoodScope : IEnumerable<KeyValuePair<string, object>>
        {
            readonly string _name;

            public FoodScope(string name)
            {
                _name = name;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                yield return new KeyValuePair<string, object>("Name", _name);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        class LuckyScope : IEnumerable<KeyValuePair<string, object>>
        {
            readonly int _luckyNumber;

            public LuckyScope(int luckyNumber)
            {
                _luckyNumber = luckyNumber;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                yield return new KeyValuePair<string, object>("LuckyNumber", _luckyNumber);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        class Person
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string FirstName { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string LastName { get; set; }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(48)]
        [InlineData(100)]
        public void LowAndHighNumberedEventIdsAreMapped(int id)
        {
            var orig = new EventId(id, "test");
            var mapped = SerilogLogger.CreateEventIdProperty(orig);
            var value = Assert.IsType<StructureValue>(mapped.Value);
            Assert.Equal(2, value.Properties.Count);
            var idValue = value.Properties.Single(p => p.Name == "Id").Value;
            var scalar = Assert.IsType<ScalarValue>(idValue);
            Assert.Equal(id, scalar.Value);
        }
        
        [Fact]
        public void MismatchedMessageTemplateParameterCountIsHandled()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            logger.LogInformation("Some test message with {Two} {Properties}", "OneProperty");

            Assert.Equal(0, sink.Writes.Count);
        }
    }
}
