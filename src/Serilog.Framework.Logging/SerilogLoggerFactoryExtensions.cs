// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Serilog.Framework.Logging;

namespace Microsoft.Extensions.Logging
{
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
        /// <returns>The logger factory.</returns>
        public static ILoggerFactory AddSerilog(
            this ILoggerFactory factory,
            Serilog.ILogger logger = null)
        {
            if (factory == null) throw new ArgumentNullException("factory");

            factory.AddProvider(new SerilogLoggerProvider(logger));

            return factory;
        }
    }
}