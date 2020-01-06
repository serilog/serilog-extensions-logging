// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;
using FrameworkLogger = Microsoft.Extensions.Logging.ILogger;
using System.Reflection;
using Serilog.Parsing;

namespace Serilog.Extensions.Logging
{
    class SerilogLogger : FrameworkLogger
    {
        readonly SerilogLoggerProvider _provider;
        readonly ILogger _logger;

        static readonly MessageTemplateParser MessageTemplateParser = new MessageTemplateParser();

        // It's rare to see large event ids, as they are category-specific
        static readonly LogEventProperty[] LowEventIdValues = Enumerable.Range(0, 48)
            .Select(n => new LogEventProperty("Id", new ScalarValue(n)))
            .ToArray();

        public SerilogLogger(
            SerilogLoggerProvider provider,
            ILogger logger = null,
            string name = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger;

            // If a logger was passed, the provider has already added itself as an enricher
            _logger = _logger ?? Serilog.Log.Logger.ForContext(new[] { provider });

            if (name != null)
            {
                _logger = _logger.ForContext(Constants.SourceContextPropertyName, name);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(LevelConvert.ToSerilogLevel(logLevel));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _provider.BeginScope(state);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var level = LevelConvert.ToSerilogLevel(logLevel);
            if (!_logger.IsEnabled(level))
            {
                return;
            }

            var logger = _logger;
            string messageTemplate = null;
            object[] propertyValues = null;
            int valuesCount = 0;

            var structure = state as IReadOnlyList<KeyValuePair<string, object>>;
            if (structure == null && state is IEnumerable<KeyValuePair<string, object>> pairs)
            {
                structure = pairs.ToList();
            }

            if (structure != null)
            {
                if (structure.Count > 0)
                {
                    propertyValues = new object[structure.Count - 1];

                    for (int i = 0; i < structure.Count; i++)
                    {
                        var property = structure[i];
                        if (property.Key == SerilogLoggerProvider.OriginalFormatPropertyName && property.Value is string value)
                        {
                            messageTemplate = value;
                            continue;
                        }

                        if (propertyValues.Length == valuesCount)
                        {
                            var extra = new object[valuesCount + 1];
                            Array.Copy(propertyValues, extra, valuesCount);
                            propertyValues = extra;
                        }

                        propertyValues[valuesCount++] = property.Value;
                    }
                }

                if (messageTemplate == null)
                {
                    var stateType = state.GetType();
                    var stateTypeInfo = stateType.GetTypeInfo();
                    // Imperfect, but at least eliminates `1 names

                    if (!stateTypeInfo.IsGenericType)
                    {
                        messageTemplate = "{" + stateType.Name + ":l}";
                        if (propertyValues == null || propertyValues.Length != 1)
                        {
                            propertyValues = new object[1];
                        }
                        propertyValues[0] = AsLoggableValue(state, formatter);
                        valuesCount = 1;
                    }
                }
            }

            if (messageTemplate == null)
            {
                if (state != null)
                {
                    messageTemplate = "{State:l}";
                }
                else if (formatter != null)
                {
                    messageTemplate = "{Message:l}";
                }

                if (messageTemplate == null)
                {
                    messageTemplate = "";
                }
                else
                {
                    if (propertyValues == null || propertyValues.Length != 1)
                    {
                        propertyValues = new object[1];
                    }
                    propertyValues[0] = AsLoggableValue(state, formatter);
                    valuesCount = 1;
                }
            }

            if (propertyValues == null)
            {
                propertyValues = Array.Empty<object>();
            }
            else if (propertyValues.Length != valuesCount)
            {
                // Trimm the array because PropertyBinder uses all elements in the array
                if (valuesCount == 0)
                {
                    propertyValues = Array.Empty<object>();
                }
                else
                {
                    var trimmed = new object[valuesCount];
                    Array.Copy(propertyValues, trimmed, valuesCount);
                    propertyValues = trimmed;
                }
            }

            if (logger.BindMessageTemplate(messageTemplate, propertyValues, out MessageTemplate parsedTemplate, out IEnumerable<LogEventProperty> properties))
            {
                if (eventId.Id != 0 || eventId.Name != null)
                {
                    var eventIdProperty = CreateEventIdProperty(eventId);
                    properties = Concat(properties, eventIdProperty);
                }

                var evt = new LogEvent(DateTimeOffset.Now, level, exception, parsedTemplate, properties);
                logger.Write(evt);
            }
        }

        static IEnumerable<T> Concat<T>(IEnumerable<T> body, T tail)
        {
            foreach (var item in body)
            {
                yield return item;
            }

            yield return tail;
        }

        static object AsLoggableValue<TState>(TState state, Func<TState, Exception, string> formatter)
        {
            object sobj = state;
            if (formatter != null)
                sobj = formatter(state, null);
            return sobj;
        }

        internal static LogEventProperty CreateEventIdProperty(EventId eventId)
        {
            var properties = new List<LogEventProperty>(2);

            if (eventId.Id != 0)
            {
                if (eventId.Id >= 0 && eventId.Id < LowEventIdValues.Length)
                    // Avoid some allocations
                    properties.Add(LowEventIdValues[eventId.Id]);
                else
                    properties.Add(new LogEventProperty("Id", new ScalarValue(eventId.Id)));
            }

            if (eventId.Name != null)
            {
                properties.Add(new LogEventProperty("Name", new ScalarValue(eventId.Name)));
            }

            return new LogEventProperty("EventId", new StructureValue(properties));
        }
    }
}