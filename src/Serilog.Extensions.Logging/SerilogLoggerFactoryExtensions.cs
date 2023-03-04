// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;

namespace Serilog;

/// <summary>
/// Extends <see cref="ILoggerFactory"/> with Serilog configuration methods.
/// </summary>
public static class SerilogLoggerFactoryExtensions
{
    /// <summary>
    /// Add Serilog to the logging pipeline.
    /// </summary>
    /// <param name="factory">The logger factory to configure.</param>
    /// <param name="logger">The Serilog logger; if not supplied, the static <see cref="Serilog.Log"/> will be used.</param>
    /// <param name="dispose">When true, dispose <paramref name="logger"/> when the framework disposes the provider. If the
    /// logger is not specified but <paramref name="dispose"/> is true, the <see cref="Log.CloseAndFlush()"/> method will be
    /// called on the static <see cref="Log"/> class instead.</param>
    /// <returns>Reference to the supplied <paramref name="factory"/>.</returns>
    public static ILoggerFactory AddSerilog(
        this ILoggerFactory factory,
        ILogger? logger = null,
        bool dispose = false)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        factory.AddProvider(new SerilogLoggerProvider(logger, dispose));

        return factory;
    }
}
