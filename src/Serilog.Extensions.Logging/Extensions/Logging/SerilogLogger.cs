// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using FrameworkLogger = Microsoft.Extensions.Logging.ILogger;
using System.Reflection;
using Serilog.Debugging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Serilog.Extensions.Logging;

sealed class SerilogLogger : FrameworkLogger
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
    readonly EventIdPropertyCache _eventIdPropertyCache = new();

    static readonly CachingMessageTemplateParser MessageTemplateParser = new();

    public SerilogLogger(
        SerilogLoggerProvider provider,
        ILogger? logger = null,
        string? name = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        // If a logger was passed, the provider has already added itself as an enricher
        _logger = logger ?? Serilog.Log.Logger.ForContext([provider]);

        if (name != null)
        {
            _logger = _logger.ForContext(Constants.SourceContextPropertyName, name);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && _logger.IsEnabled(LevelConvert.ToSerilogLevel(logLevel));
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return _provider.BeginScope(state);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
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

        LogEvent? evt = null;
        try
        {
            evt = PrepareWrite(level, eventId, state, exception, formatter);
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine($"Failed to write event through {nameof(SerilogLogger)}: {ex}");
        }

        // Do not swallow exceptions from here because Serilog takes care of them in case of WriteTo and throws them back to the caller in case of AuditTo.
        if (evt != null)
            _logger.Write(evt);
    }

    LogEvent PrepareWrite<TState>(LogEventLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string? messageTemplate = null;

        var properties = new Dictionary<string, LogEventPropertyValue>();

        // Optimization: MEL state object type represents either LogValues or FormattedLogValues
        // These types implement IReadOnlyList, which be used to avoid enumerator obj allocation.
        if (state is IReadOnlyList<KeyValuePair<string, object?>> propertiesList)
        {
            var length = propertiesList.Count;

            for (var i = 0; i < length; i++)
            {
                ProcessStateProperty(propertiesList[i]);
            }

            TrySetMessageTemplateFromState();
        }
        else if (state is IEnumerable<KeyValuePair<string, object?>> propertiesEnumerable)
        {
            foreach (var property in propertiesEnumerable)
            {
                ProcessStateProperty(property);
            }

            TrySetMessageTemplateFromState();
        }

        if (messageTemplate == null)
        {
            string? propertyName = null;
            if (state != null)
            {
                propertyName = "State";
                messageTemplate = "{State:l}";
            }
            // `formatter` was originally accepted as nullable, so despite the new annotation, this check should still
            // be made.
            else if (formatter != null!)
            {
                propertyName = "Message";
                messageTemplate = "{Message:l}";
            }

            if (propertyName != null)
            {
                if (_logger.BindProperty(propertyName, AsLoggableValue(state, formatter!), false, out var property))
                    properties[property.Name] = property.Value;
            }
        }

        // The overridden `!=` operator on this type ignores `Name`.
        if (eventId.Id != 0 || eventId.Name != null)
            properties["EventId"] = _eventIdPropertyCache.GetOrCreatePropertyValue(in eventId);

        var (traceId, spanId) = Activity.Current is { } activity ?
            (activity.TraceId, activity.SpanId) :
            (default(ActivityTraceId), default(ActivitySpanId));

        var parsedTemplate = messageTemplate != null ? MessageTemplateParser.Parse(messageTemplate) : MessageTemplate.Empty;
        return LogEvent.UnstableAssembleFromParts(DateTimeOffset.Now, level, exception, parsedTemplate, properties, traceId, spanId);

        void ProcessStateProperty(KeyValuePair<string, object?> property)
        {
            if (property is { Key: SerilogLoggerProvider.OriginalFormatPropertyName, Value: string value })
            {
                messageTemplate = value;
            }
            else if (property.Key.StartsWith('@'))
            {
                if (this._logger.BindProperty(GetKeyWithoutFirstSymbol(DestructureDictionary, property.Key), property.Value, true, out var destructured))
                    properties[destructured.Name] = destructured.Value;
            }
            else if (property.Key.StartsWith('$'))
            {
                if (this._logger.BindProperty(GetKeyWithoutFirstSymbol(StringifyDictionary, property.Key), property.Value?.ToString(), true, out var stringified))
                    properties[stringified.Name] = stringified.Value;
            }
            else
            {
                // Simple micro-optimization for the most common and reliably scalar values; could go further here.
                if (property.Value is null or string or int or long && LogEventProperty.IsValidName(property.Key))
                    properties[property.Key] = new ScalarValue(property.Value);
                else if (this._logger.BindProperty(property.Key, property.Value, false, out var bound))
                    properties[bound.Name] = bound.Value;
            }
        }

        void TrySetMessageTemplateFromState()
        {
            // Imperfect, but at least eliminates `1 names
            var stateType = state.GetType();
            var stateTypeInfo = stateType.GetTypeInfo();
            // Imperfect, but at least eliminates `1 names
            if (messageTemplate == null && !stateTypeInfo.IsGenericType)
            {
                messageTemplate = "{" + stateType.Name + ":l}";
                if (_logger.BindProperty(stateType.Name, AsLoggableValue(state, formatter), false, out var stateTypeProperty))
                    properties[stateTypeProperty.Name] = stateTypeProperty.Value;
            }
        }
    }

    static object? AsLoggableValue<TState>(TState state, Func<TState, Exception?, string>? formatter)
    {
        object? stateObj = null;
        if (formatter != null)
            stateObj = formatter(state, null);
        return stateObj ?? state;
    }
}
