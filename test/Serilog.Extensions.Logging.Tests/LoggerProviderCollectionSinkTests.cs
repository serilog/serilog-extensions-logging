// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging.Tests.Support;
using Xunit;

namespace Serilog.Extensions.Logging.Tests
{
    public class LoggerProviderCollectionSinkTests
    {
        const string Name = "test";
        const string TestMessage = "This is a test";

        static Tuple<SerilogLogger, ExtensionsProvider> SetUp(LogLevel logLevel)
        {
            var providers = new LoggerProviderCollection();
            var provider = new ExtensionsProvider(logLevel);
            providers.AddProvider(provider);
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.Providers(providers)
                .MinimumLevel.Is(LevelConvert.ToSerilogLevel(logLevel))
                .CreateLogger();

            var logger = (SerilogLogger)new SerilogLoggerProvider(serilogLogger).CreateLogger(Name);

            return new Tuple<SerilogLogger, ExtensionsProvider>(logger, provider);
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
            Assert.Equal(LogLevel.Trace, sink.Writes[0].logLevel);
            Assert.Equal(LogLevel.Debug, sink.Writes[1].logLevel);
            Assert.Equal(LogLevel.Information, sink.Writes[2].logLevel);
            Assert.Equal(LogLevel.Warning, sink.Writes[3].logLevel);
            Assert.Equal(LogLevel.Error, sink.Writes[4].logLevel);
            Assert.Equal(LogLevel.Critical, sink.Writes[5].logLevel);
        }

        [Fact]
        public void LogsCorrectEventId()
        {
            var (logger, sink) = SetUp(LogLevel.Trace);

            logger.Log(LogLevel.Trace, new EventId(1, nameof(LogLevel.Trace)), TestMessage, null, null);
            logger.Log(LogLevel.Debug, new EventId(2, nameof(LogLevel.Debug)), TestMessage, null, null);
            logger.Log(LogLevel.Information, new EventId(3, nameof(LogLevel.Information)), TestMessage, null, null);
            logger.Log(LogLevel.Warning, new EventId(4, nameof(LogLevel.Warning)), TestMessage, null, null);
            logger.Log(LogLevel.Error, new EventId(5, nameof(LogLevel.Error)), TestMessage, null, null);
            logger.Log(LogLevel.Critical, new EventId(6, nameof(LogLevel.Critical)), TestMessage, null, null);

            Assert.Equal(6, sink.Writes.Count);

            Assert.Equal(1, sink.Writes[0].eventId.Id);
            Assert.Equal(nameof(LogLevel.Trace), sink.Writes[0].eventId.Name);

            Assert.Equal(2, sink.Writes[1].eventId.Id);
            Assert.Equal(nameof(LogLevel.Debug), sink.Writes[1].eventId.Name);

            Assert.Equal(3, sink.Writes[2].eventId.Id);
            Assert.Equal(nameof(LogLevel.Information), sink.Writes[2].eventId.Name);

            Assert.Equal(4, sink.Writes[3].eventId.Id);
            Assert.Equal(nameof(LogLevel.Warning), sink.Writes[3].eventId.Name);

            Assert.Equal(5, sink.Writes[4].eventId.Id);
            Assert.Equal(nameof(LogLevel.Error), sink.Writes[4].eventId.Name);

            Assert.Equal(6, sink.Writes[5].eventId.Id);
            Assert.Equal(nameof(LogLevel.Critical), sink.Writes[5].eventId.Name);
        }
    }
}
