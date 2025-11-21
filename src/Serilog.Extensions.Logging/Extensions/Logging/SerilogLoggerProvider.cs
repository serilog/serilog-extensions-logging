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
public sealed class SerilogLoggerProvider : ILoggerProvider, ILogEventEnricher, ISupportExternalScope
#if FEATURE_ASYNCDISPOSABLE
    , IAsyncDisposable
#endif
{
    internal const string OriginalFormatPropertyName = "{OriginalFormat}";
    internal const string ScopePropertyName = "Scope";

    // May be null; if it is, Log.Logger will be lazily used
    readonly ILogger? _logger;
    readonly Action? _dispose;
    readonly ThreadLocal<ScopeCollector> _scopeCollector = new(() => new ScopeCollector());
#if FEATURE_ASYNCDISPOSABLE
    readonly Func<ValueTask>? _disposeAsync;
#endif
    IExternalScopeProvider? _externalScopeProvider;

    /// <summary>
    /// Construct a <see cref="SerilogLoggerProvider"/>.
    /// </summary>
    /// <param name="logger">A Serilog logger to pipe events through; if null, the static <see cref="Log"/> class will be used.</param>
    /// <param name="dispose">If true, the provided logger or static log class will be disposed/closed when the provider is disposed.</param>
    public SerilogLoggerProvider(ILogger? logger = null, bool dispose = false)
    {
        if (logger != null)
            _logger = logger.ForContext([this]);

        if (dispose)
        {
            if (logger != null)
            {
                _dispose = () => (logger as IDisposable)?.Dispose();
#if FEATURE_ASYNCDISPOSABLE
                _disposeAsync = async () =>
                {
                    if (logger is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else
                    {
                        (logger as IDisposable)?.Dispose();
                    }
                };
#endif
            }
            else
            {
                _dispose = Log.CloseAndFlush;
#if FEATURE_ASYNCDISPOSABLE
                _disposeAsync = Log.CloseAndFlushAsync;
#endif
            }
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
        var scopeCollector = _scopeCollector.Value!;

        for (var scope = CurrentScope; scope != null; scope = scope.Parent)
        {
            scope.EnrichAndCreateScopeItem(logEvent, propertyFactory, out var scopeItem);

            if (scopeItem != null)
            {
                scopeCollector.AddItem(scopeItem);
            }
        }

        scopeCollector.ReverseItems();

        _externalScopeProvider?.ForEachScope(static (state, parameters) =>
        {
            SerilogLoggerScope.EnrichWithStateAndCreateScopeItem(
                parameters.LogEvent,
                parameters.PropertyFactory,
                state,
                update: true,
                out var scopeItem);

            if (scopeItem != null)
            {
                parameters.ScopeCollector.AddItem(scopeItem);
            }
        }, (ScopeCollector: scopeCollector, PropertyFactory: propertyFactory, LogEvent: logEvent));

        if (scopeCollector.Complete() is { } items)
        {
            logEvent.AddPropertyIfAbsent(new LogEventProperty(ScopePropertyName, new SequenceValue(items)));
        }
    }

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _externalScopeProvider = scopeProvider;
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

#if FEATURE_ASYNCDISPOSABLE
    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return _disposeAsync?.Invoke() ?? default;
    }
#endif

    /// <summary>
    /// A wrapper around a list to allow lazy initialization when iterating through scopes.
    /// </summary>
    sealed class ScopeCollector
    {
        List<LogEventPropertyValue>? _scopeItems;

        public void AddItem(LogEventPropertyValue scopeItem)
        {
            _scopeItems ??= [];
            _scopeItems.Add(scopeItem);
        }

        public void ReverseItems() => _scopeItems?.Reverse();

        public List<LogEventPropertyValue>? Complete()
        {
            var scopeItems = _scopeItems;
            _scopeItems = null;
            return scopeItems;
        }
    }
}
