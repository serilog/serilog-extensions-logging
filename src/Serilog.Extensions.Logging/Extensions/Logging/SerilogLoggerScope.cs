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

    public void EnrichAndCreateScopeItem(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, out LogEventPropertyValue? scopeItem)
    {
        void AddProperty(KeyValuePair<string, object> stateProperty)
        {
            var key = stateProperty.Key;
            var destructureObject = false;
            var value = stateProperty.Value;

            if (key.StartsWith("@"))
            {
                key = SerilogLogger.GetKeyWithoutFirstSymbol(SerilogLogger.DestructureDictionary, key);
                destructureObject = true;
            }
            else if (key.StartsWith("$"))
            {
                key = SerilogLogger.GetKeyWithoutFirstSymbol(SerilogLogger.StringifyDictionary, key);
                value = value?.ToString();
            }

            var property = propertyFactory.CreateProperty(key, value, destructureObject);
            logEvent.AddPropertyIfAbsent(property);
        }

        if (_state == null)
        {
            scopeItem = null;
            return;
        }

        // Eliminates boxing of Dictionary<TKey, TValue>.Enumerator for the most common use case
        if (_state is Dictionary<string, object> dictionary)
        {
            scopeItem = null; // Unless it's `FormattedLogValues`, these are treated as property bags rather than scope items.

            foreach (var stateProperty in dictionary)
            {
                if (stateProperty.Key == SerilogLoggerProvider.OriginalFormatPropertyName && stateProperty.Value is string)
                    scopeItem = new ScalarValue(_state.ToString());
                else
                    AddProperty(stateProperty);
            }
        }
        else if (_state is IEnumerable<KeyValuePair<string, object>> stateProperties)
        {
            scopeItem = null; // Unless it's `FormattedLogValues`, these are treated as property bags rather than scope items.

            foreach (var stateProperty in stateProperties)
            {
                if (stateProperty.Key == SerilogLoggerProvider.OriginalFormatPropertyName && stateProperty.Value is string)
                    scopeItem = new ScalarValue(_state.ToString());
                else
                    AddProperty(stateProperty);
            }
        }
        else
        {
            scopeItem = propertyFactory.CreateProperty(NoName, _state).Value;
        }
    }
}
