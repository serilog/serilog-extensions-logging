// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Serilog.Core;
using Serilog.Events;

namespace Serilog.Extensions.Logging;

class SerilogLoggerScope : IDisposable
{
    const string NoName = "None";

    readonly SerilogLoggerProvider _provider;
    readonly object? _state;
    readonly IDisposable? _chainedDisposable;

    // An optimization only, no problem if there are data races on this.
    bool _disposed;

    public SerilogLoggerScope(SerilogLoggerProvider provider, object? state, IDisposable? chainedDisposable = null)
    {
        _provider = provider;
        _state = state;

        Parent = _provider.CurrentScope;
        _provider.CurrentScope = this;
        _chainedDisposable = chainedDisposable;
    }

    public SerilogLoggerScope? Parent { get; }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            // In case one of the parent scopes has been disposed out-of-order, don't
            // just blindly reinstate our own parent.
            for (var scan = _provider.CurrentScope; scan != null; scan = scan.Parent)
            {
                if (ReferenceEquals(scan, this))
                    _provider.CurrentScope = Parent;
            }

            _chainedDisposable?.Dispose();
        }
    }

    public void EnrichAndCreateScopeItem(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, out LogEventPropertyValue? scopeItem) => EnrichWithStateAndCreateScopeItem(logEvent, propertyFactory, _state, out scopeItem);

    public static void EnrichWithStateAndCreateScopeItem(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, object? state, out LogEventPropertyValue? scopeItem)
    {
        if (state == null)
        {
            scopeItem = null;
            return;
        }

        // Eliminates boxing of Dictionary<TKey, TValue>.Enumerator for the most common use case
        if (state is Dictionary<string, object> dictionary)
        {
            // Separate handling of this case eliminates boxing of Dictionary<TKey, TValue>.Enumerator.
            scopeItem = null;
            foreach (var stateProperty in dictionary)
            {                
                AddProperty(logEvent, propertyFactory, stateProperty.Key, stateProperty.Value);
            }
        }
        else if (state is IEnumerable<KeyValuePair<string, object>> stateProperties)
        {
            scopeItem = null;
            foreach (var stateProperty in stateProperties)
            {
                if (stateProperty is { Key: SerilogLoggerProvider.OriginalFormatPropertyName, Value: string })
                {
                    // `_state` is most likely `FormattedLogValues` (a MEL internal type).
                    scopeItem = new ScalarValue(state.ToString());
                }
                else
                {
                    AddProperty(logEvent, propertyFactory, stateProperty.Key, stateProperty.Value);
                }
            }
        }
#if FEATURE_ITUPLE
        else if (state is System.Runtime.CompilerServices.ITuple tuple && tuple.Length == 2 && tuple[0] is string s)
        {
            scopeItem = null; // Unless it's `FormattedLogValues`, these are treated as property bags rather than scope items.
            AddProperty(logEvent, propertyFactory, s, tuple[1]);
        }
#else
        else if (state is ValueTuple<string, object?> tuple)
        {
            scopeItem = null;
            AddProperty(logEvent, propertyFactory, tuple.Item1, tuple.Item2);
        }
#endif
        else
        {
            scopeItem = propertyFactory.CreateProperty(NoName, state).Value;
        }
    }

    static void AddProperty(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, string key, object? value)
    {
        var destructureObject = false;

        if (key.StartsWith("@", StringComparison.Ordinal))
        {
            key = SerilogLogger.GetKeyWithoutFirstSymbol(SerilogLogger.DestructureDictionary, key);
            destructureObject = true;
        }
        else if (key.StartsWith("$", StringComparison.Ordinal))
        {
            key = SerilogLogger.GetKeyWithoutFirstSymbol(SerilogLogger.StringifyDictionary, key);
            value = value?.ToString();
        }

        var property = propertyFactory.CreateProperty(key, value, destructureObject);
        logEvent.AddPropertyIfAbsent(property);
    }
}
