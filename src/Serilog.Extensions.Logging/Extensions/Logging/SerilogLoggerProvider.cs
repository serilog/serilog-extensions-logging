// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using FrameworkLogger = Microsoft.Extensions.Logging.ILogger;
using Serilog.Context;

namespace Serilog.Extensions.Logging;

/// <summary>
/// An <see cref="ILoggerProvider"/> that pipes events through Serilog.
/// </summary>
[ProviderAlias("Serilog")]
public class SerilogLoggerProvider : ILoggerProvider, ILogEventEnricher
{
    internal const string OriginalFormatPropertyName = "{OriginalFormat}";
    internal const string ScopePropertyName = "Scope";

    // May be null; if it is, Log.Logger will be lazily used
    readonly ILogger? _logger;
    readonly Action? _dispose;

    /// <summary>
    /// Construct a <see cref="SerilogLoggerProvider"/>.
    /// </summary>
    /// <param name="logger">A Serilog logger to pipe events through; if null, the static <see cref="Log"/> class will be used.</param>
    /// <param name="dispose">If true, the provided logger or static log class will be disposed/closed when the provider is disposed.</param>
    public SerilogLoggerProvider(ILogger? logger = null, bool dispose = false)
    {
        if (logger != null)
            _logger = logger.ForContext(new[] { this });

        if (dispose)
        {
            if (logger != null)
                _dispose = () => (logger as IDisposable)?.Dispose();
            else
                _dispose = Log.CloseAndFlush;
        }
    }

    /// <inheritdoc />
    public FrameworkLogger CreateLogger(string name)
    {
        return new SerilogLogger(this, _logger, name);
    }

    /// <inheritdoc cref="IDisposable" />
    public IDisposable BeginScope<T>(T state)
    {
        if (CurrentScope != null)
            return new SerilogLoggerScope(this, state);

        // The outermost scope pushes and pops the Serilog `LogContext` - once
        // this enricher is on the stack, the `CurrentScope` property takes care
        // of the rest of the `BeginScope()` stack.
        var popSerilogContext = LogContext.Push(this);
        return new SerilogLoggerScope(this, state, popSerilogContext);
    }

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        List<LogEventPropertyValue>? scopeItems = null;
        for (var scope = CurrentScope; scope != null; scope = scope.Parent)
        {
            scope.EnrichAndCreateScopeItem(logEvent, propertyFactory, out var scopeItem);

            if (scopeItem != null)
            {
                scopeItems ??= new List<LogEventPropertyValue>();
                scopeItems.Add(scopeItem);
            }
        }

        if (scopeItems != null)
        {
            scopeItems.Reverse();
            logEvent.AddPropertyIfAbsent(new LogEventProperty(ScopePropertyName, new SequenceValue(scopeItems)));
        }
    }

    readonly AsyncLocal<SerilogLoggerScope?> _value = new();

    internal SerilogLoggerScope? CurrentScope
    {
        get => _value.Value;
        set => _value.Value = value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dispose?.Invoke();
    }
}
