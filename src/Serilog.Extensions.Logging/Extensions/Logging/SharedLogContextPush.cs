// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Serilog.Context;
using Serilog.Core;

namespace Serilog.Extensions.Logging;

/// <summary>
/// Pushes enrichers onto the Serilog <see cref="LogContext"/> on behalf of any number of
/// <see cref="SerilogLoggerProvider"/>s in the current execution context, restoring the
/// <see cref="LogContext"/> only once, to its original state, when the last of them is done.
/// </summary>
/// <remarks>
/// <see cref="LogContext.Push(ILogEventEnricher)"/> is disposed by restoring a snapshot of the context.
/// When multiple providers push in turn, and their scopes are disposed in the same order (as
/// Microsoft.Extensions.Logging does), restoring the later snapshot would reinstate the enrichers
/// pushed by the earlier providers, so that they pile up on the context indefinitely.
/// </remarks>
sealed class SharedLogContextPush
{
    static readonly AsyncLocal<SharedLogContextPush?> Current = new();

    readonly IDisposable _restoreLogContext;
    int _holders;

    SharedLogContextPush(IDisposable restoreLogContext)
    {
        _restoreLogContext = restoreLogContext;
        _holders = 1;
    }

    public static IDisposable Push(ILogEventEnricher enricher)
    {
        var current = Current.Value;
        if (current != null && current.TryAcquire())
        {
            // The snapshot taken by the first push is the one to restore, so this one is never disposed.
            _ = LogContext.Push(enricher);
            return new Holder(current);
        }

        var shared = new SharedLogContextPush(LogContext.Push(enricher));
        Current.Value = shared;
        return new Holder(shared);
    }

    bool TryAcquire()
    {
        int holders;
        do
        {
            holders = Volatile.Read(ref _holders);
            if (holders == 0)
                return false;
        }
        while (Interlocked.CompareExchange(ref _holders, holders + 1, holders) != holders);

        return true;
    }

    void Release()
    {
        if (Interlocked.Decrement(ref _holders) != 0)
            return;

        if (ReferenceEquals(Current.Value, this))
            Current.Value = null;

        _restoreLogContext.Dispose();
    }

    sealed class Holder : IDisposable
    {
        SharedLogContextPush? _shared;

        public Holder(SharedLogContextPush shared) => _shared = shared;

        public void Dispose() => Interlocked.Exchange(ref _shared, null)?.Release();
    }
}
