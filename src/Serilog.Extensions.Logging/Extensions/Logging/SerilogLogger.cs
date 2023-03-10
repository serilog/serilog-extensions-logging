// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using FrameworkLogger = Microsoft.Extensions.Logging.ILogger;
using System.Reflection;
using Serilog.Debugging;
using System.Collections.Concurrent;

namespace Serilog.Extensions.Logging;

class SerilogLogger : FrameworkLogger
{
    internal static readonly ConcurrentDictionary<string, string> DestructureDictionary = new();
    internal static readonly ConcurrentDictionary<string, string> StringifyDictionary = new();

    internal static string GetKeyWithoutFirstSymbol(ConcurrentDictionary<string, string> source, string key)
    {
        if (source.TryGetValue(key, out var value))
            return value;
        if (source.Count < 1000)
            return source.GetOrAdd(key, k => k.Substring(1));
        return key.Substring(1);
    }

    readonly SerilogLoggerProvider _provider;
    readonly ILogger _logger;

    static readonly CachingMessageTemplateParser MessageTemplateParser = new();

    // It's rare to see large event ids, as they are category-specific
    static readonly LogEventProperty[] LowEventIdValues = Enumerable.Range(0, 48)
        .Select(n => new LogEventProperty("Id", new ScalarValue(n)))
        .ToArray();

    public SerilogLogger(
        SerilogLoggerProvider provider,
        ILogger? logger = null,
        string? name = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger!;

        // If a logger was passed, the provider has already added itself as an enricher
        _logger ??= Serilog.Log.Logger.ForContext(new[] { provider });

        if (name != null)
        {
            _logger = _logger.ForContext(Constants.SourceContextPropertyName, name);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && _logger.IsEnabled(LevelConvert.ToSerilogLevel(logLevel));
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return _provider.BeginScope(state);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (logLevel == LogLevel.None)
        {
            return;
        }
        var level = LevelConvert.ToSerilogLevel(logLevel);
        if (!_logger.IsEnabled(level))
        {
            return;
        }

        bool parsed = false;
        try
        {
            Write(level, eventId, state, exception, formatter, out parsed);
        }
        catch (Exception ex) when (!parsed)
        {
            SelfLog.WriteLine($"Failed to write event through {typeof(SerilogLogger).Name}: {ex}");
        }
    }

    void Write<TState>(LogEventLevel level, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter, out bool parsed)
    {
        parsed = false;
        var logger = _logger;
        string? messageTemplate = null;

        var properties = new List<LogEventProperty>();

        if (state is IEnumerable<KeyValuePair<string, object>> structure)
        {
            foreach (var property in structure)
            {
                if (property.Key == SerilogLoggerProvider.OriginalFormatPropertyName && property.Value is string value)
                {
                    messageTemplate = value;
                }
                else if (property.Key.StartsWith("@"))
                {
                    if (logger.BindProperty(GetKeyWithoutFirstSymbol(DestructureDictionary, property.Key), property.Value, true, out var destructured))
                        properties.Add(destructured);
                }
                else if (property.Key.StartsWith("$"))
                {
                    if (logger.BindProperty(GetKeyWithoutFirstSymbol(StringifyDictionary, property.Key), property.Value?.ToString(), true, out var stringified))
                        properties.Add(stringified);
                }
                else
                {
                    if (logger.BindProperty(property.Key, property.Value, false, out var bound))
                        properties.Add(bound);
                }
            }

            var stateType = state.GetType();
            var stateTypeInfo = stateType.GetTypeInfo();
            // Imperfect, but at least eliminates `1 names
            if (messageTemplate == null && !stateTypeInfo.IsGenericType)
            {
                messageTemplate = "{" + stateType.Name + ":l}";
                if (logger.BindProperty(stateType.Name, AsLoggableValue(state, formatter), false, out var stateTypeProperty))
                    properties.Add(stateTypeProperty);
            }
        }

        if (messageTemplate == null)
        {
            string? propertyName = null;
            if (state != null)
            {
                propertyName = "State";
                messageTemplate = "{State:l}";
            }
            else if (formatter != null)
            {
                propertyName = "Message";
                messageTemplate = "{Message:l}";
            }

            if (propertyName != null)
            {
                if (logger.BindProperty(propertyName, AsLoggableValue(state, formatter!), false, out var property))
                    properties.Add(property);
            }
        }

        if (eventId.Id != 0 || eventId.Name != null)
            properties.Add(CreateEventIdProperty(eventId));

        var parsedTemplate = MessageTemplateParser.Parse(messageTemplate ?? "");
        var evt = new LogEvent(DateTimeOffset.Now, level, exception, parsedTemplate, properties);
        parsed = true;
        logger.Write(evt);
    }

    static object? AsLoggableValue<TState>(TState state, Func<TState, Exception, string> formatter)
    {
        object? sobj = state;
        if (formatter != null)
            sobj = formatter(state, null!);
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
