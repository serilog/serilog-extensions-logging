// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Serilog.Extensions.Logging.Tests.Support
{
    public class ExtensionsProvider : ILoggerProvider, Microsoft.Extensions.Logging.ILogger
    {
        private readonly LogLevel enabledLevel;
        public List<(LogLevel logLevel, EventId eventId, object state, Exception exception, string message)> Writes { get; set; } = new();

        public ExtensionsProvider(LogLevel enabledLevel)
        {
            this.enabledLevel = enabledLevel;
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return enabledLevel <= logLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Writes.Add((logLevel, eventId, state, exception, formatter(state, exception)));
        }

        public void Dispose()
        {
        }
    }
}