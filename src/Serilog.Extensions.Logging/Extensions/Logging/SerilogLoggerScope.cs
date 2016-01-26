// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Serilog.Extensions.Logging
{
    public class SerilogLoggerScope : IDisposable
    {
        private readonly SerilogLoggerProvider _provider;

        public SerilogLoggerScope(SerilogLoggerProvider provider, string name, object state)
        {
            _provider = provider;
            Name = name;
            State = state;

            Parent = _provider.CurrentScope;
            _provider.CurrentScope = this;
        }

        public SerilogLoggerScope Parent { get; }
        public string Name { get; private set; }
        public object State { get; private set; }

        public void RemoveScope()
        {
            for (var scan = _provider.CurrentScope; scan != null; scan = scan.Parent)
            {
                if (ReferenceEquals(scan, this))
                {
                    _provider.CurrentScope = Parent;
                }
            }
        }

        private bool _disposedValue; // To detect redundant calls

        public void Dispose()
        {
            if (!_disposedValue)
            {
                RemoveScope();
            }
            _disposedValue = true;
        }
    }
}
