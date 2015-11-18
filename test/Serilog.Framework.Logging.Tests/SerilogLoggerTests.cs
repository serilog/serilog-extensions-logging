// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Serilog.Events;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Collections.Generic;

namespace Serilog.Framework.Logging.Test
{
    [TestFixture]
    public class SerilogLoggerTest
    {
        private const string _name = "test";
        private const string _state = "This is a test";
        private static readonly Func<object, Exception, string> TheMessageAndError = (message, error) => string.Format(CultureInfo.CurrentCulture, "{0}:{1}", message, error);

        private Tuple<SerilogLogger, SerilogSink> SetUp(LogLevel logLevel)
        {
            var sink = new SerilogSink();

            var config = new LoggerConfiguration()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .WriteTo.Sink(sink);

            SetMinLevel(config, logLevel);

            var provider = new SerilogLoggerProvider(config.CreateLogger());
            var logger = (SerilogLogger)provider.CreateLogger(_name);

            return new Tuple<SerilogLogger, SerilogSink>(logger, sink);
        }

        private LoggerConfiguration SetMinLevel(LoggerConfiguration serilog, LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    return serilog.MinimumLevel.Verbose();
                case LogLevel.Verbose:
                    return serilog.MinimumLevel.Debug();
                case LogLevel.Information:
                    return serilog.MinimumLevel.Information();
                case LogLevel.Warning:
                    return serilog.MinimumLevel.Warning();
                case LogLevel.Error:
                    return serilog.MinimumLevel.Error();
                case LogLevel.Critical:
                    return serilog.MinimumLevel.Fatal();
                default:
                    return serilog.MinimumLevel.Verbose();
            }
        }

        [Test]
        public void LogsWhenNullFilterGiven()
        {
            var t = SetUp(LogLevel.Verbose);
            var logger = t.Item1;
            var sink = t.Item2;

            logger.Log(LogLevel.Information, 0, _state, null, null);

            Assert.AreEqual(1, sink.Writes.Count);
        }

        [Test]
        public void LogsCorrectLevel()
        {
            var t = SetUp(LogLevel.Debug);
            var logger = t.Item1;
            var sink = t.Item2;

            logger.Log(LogLevel.Debug, 0, _state, null, null);
            logger.Log(LogLevel.Verbose, 0, _state, null, null);
            logger.Log(LogLevel.Information, 0, _state, null, null);
            logger.Log(LogLevel.Warning, 0, _state, null, null);
            logger.Log(LogLevel.Error, 0, _state, null, null);
            logger.Log(LogLevel.Critical, 0, _state, null, null);

            Assert.AreEqual(6, sink.Writes.Count);
            Assert.AreEqual(LogEventLevel.Verbose, sink.Writes[0].Level);
            Assert.AreEqual(LogEventLevel.Debug, sink.Writes[1].Level);
            Assert.AreEqual(LogEventLevel.Information, sink.Writes[2].Level);
            Assert.AreEqual(LogEventLevel.Warning, sink.Writes[3].Level);
            Assert.AreEqual(LogEventLevel.Error, sink.Writes[4].Level);
            Assert.AreEqual(LogEventLevel.Fatal, sink.Writes[5].Level);
        }

        [Test]
        [TestCase(LogLevel.Verbose, LogLevel.Verbose, 1)]
        [TestCase(LogLevel.Verbose, LogLevel.Information, 1)]
        [TestCase(LogLevel.Verbose, LogLevel.Warning, 1)]
        [TestCase(LogLevel.Verbose, LogLevel.Error, 1)]
        [TestCase(LogLevel.Verbose, LogLevel.Critical, 1)]
        [TestCase(LogLevel.Information, LogLevel.Verbose, 0)]
        [TestCase(LogLevel.Information, LogLevel.Information, 1)]
        [TestCase(LogLevel.Information, LogLevel.Warning, 1)]
        [TestCase(LogLevel.Information, LogLevel.Error, 1)]
        [TestCase(LogLevel.Information, LogLevel.Critical, 1)]
        [TestCase(LogLevel.Warning, LogLevel.Verbose, 0)]
        [TestCase(LogLevel.Warning, LogLevel.Information, 0)]
        [TestCase(LogLevel.Warning, LogLevel.Warning, 1)]
        [TestCase(LogLevel.Warning, LogLevel.Error, 1)]
        [TestCase(LogLevel.Warning, LogLevel.Critical, 1)]
        [TestCase(LogLevel.Error, LogLevel.Verbose, 0)]
        [TestCase(LogLevel.Error, LogLevel.Information, 0)]
        [TestCase(LogLevel.Error, LogLevel.Warning, 0)]
        [TestCase(LogLevel.Error, LogLevel.Error, 1)]
        [TestCase(LogLevel.Error, LogLevel.Critical, 1)]
        [TestCase(LogLevel.Critical, LogLevel.Verbose, 0)]
        [TestCase(LogLevel.Critical, LogLevel.Information, 0)]
        [TestCase(LogLevel.Critical, LogLevel.Warning, 0)]
        [TestCase(LogLevel.Critical, LogLevel.Error, 0)]
        [TestCase(LogLevel.Critical, LogLevel.Critical, 1)]
        public void LogsWhenEnabled(LogLevel minLevel, LogLevel logLevel, int expected)
        {
            var t = SetUp(minLevel);
            var logger = t.Item1;
            var sink = t.Item2;

            logger.Log(logLevel, 0, _state, null, null);

            Assert.AreEqual(expected, sink.Writes.Count);
        }


        [Test]
        public void LogsCorrectMessage()
        {
            var t = SetUp(LogLevel.Verbose);
            var logger = t.Item1;
            var sink = t.Item2;

            var exception = new Exception();

            logger.Log(LogLevel.Information, 0, null, null, null);
            logger.Log(LogLevel.Information, 0, _state, null, null);

            Assert.AreEqual(1, sink.Writes.Count);
            Assert.AreEqual(_state, sink.Writes[0].RenderMessage());
        }

        [Test]
        public void SingleScopeProperty()
        {
            var t = SetUp(LogLevel.Verbose);
            var logger = t.Item1;
            var sink = t.Item2;

            using (logger.BeginScopeImpl(new FoodScope("pizza")))
            {
                logger.Log(LogLevel.Information, 0, _state, null, null);
            }

            Assert.AreEqual(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("Name"));
            Assert.AreEqual("\"pizza\"", sink.Writes[0].Properties["Name"].ToString());
        }

        [Test]
        public void NestedScopeSameProperty()
        {
            var t = SetUp(LogLevel.Verbose);
            var logger = t.Item1;
            var sink = t.Item2;

            using (logger.BeginScopeImpl(new FoodScope("avocado")))
            {
                using (logger.BeginScopeImpl(new FoodScope("bacon")))
                {
                    logger.Log(LogLevel.Information, 0, _state, null, null);
                }
            }

            // Should retain the property of the most specific scope
            Assert.AreEqual(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("Name"));
            Assert.AreEqual("\"bacon\"", sink.Writes[0].Properties["Name"].ToString());
        }

        [Test]
        public void NestedScopesDifferentProperties()
        {
            var t = SetUp(LogLevel.Verbose);
            var logger = t.Item1;
            var sink = t.Item2;

            using (logger.BeginScopeImpl(new FoodScope("spaghetti")))
            {
                using (logger.BeginScopeImpl(new LuckyScope(7)))
                {
                    logger.Log(LogLevel.Information, 0, _state, null, null);
                }
            }

            Assert.AreEqual(1, sink.Writes.Count);
            Assert.True(sink.Writes[0].Properties.ContainsKey("Name"));
            Assert.AreEqual("\"spaghetti\"", sink.Writes[0].Properties["Name"].ToString());
            Assert.True(sink.Writes[0].Properties.ContainsKey("LuckyNumber"));
            Assert.AreEqual("7", sink.Writes[0].Properties["LuckyNumber"].ToString());
        }

        private class FoodScope : ILogValues
        {
            private string _name;

            public FoodScope(string name)
            {
                _name = name;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                yield return new KeyValuePair<string, object>("Name", _name);
            }
        }

        private class LuckyScope : ILogValues
        {
            private int _luckyNumber;

            public LuckyScope(int luckyNumber)
            {
                _luckyNumber = luckyNumber;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                yield return new KeyValuePair<string, object>("LuckyNumber", _luckyNumber);
            }
        }
    }
}