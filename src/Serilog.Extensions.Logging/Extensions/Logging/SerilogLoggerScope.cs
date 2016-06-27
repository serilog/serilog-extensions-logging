// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Extensions.Logging
{
    class SerilogLoggerScope : IDisposable, ILogEventEnricher
    {
        readonly SerilogLoggerProvider _provider;
        readonly object _state;
        readonly IDisposable _popSerilogContext;

        // An optimization only, no problem if there are data races on this.
        bool _disposed;

        public SerilogLoggerScope(SerilogLoggerProvider provider, object state)
        {
            _provider = provider;
            _state = state;

            Parent = _provider.CurrentScope;
            _provider.CurrentScope = this;
            _popSerilogContext = LogContext.PushProperties(this);
        }

        public SerilogLoggerScope Parent { get; }
        
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

                _popSerilogContext.Dispose();
            }
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var stateProperties = _state as IEnumerable<KeyValuePair<string, object>>;
            if (stateProperties != null)
            {
                foreach (var stateProperty in stateProperties)
                {
                    if (stateProperty.Key == SerilogLoggerProvider.OriginalFormatPropertyName && stateProperty.Value is string)
                        continue;

                    var property = propertyFactory.CreateProperty(stateProperty.Key, stateProperty.Value);
                    logEvent.AddPropertyIfAbsent(property);
                }
            }
        }
    }
}
