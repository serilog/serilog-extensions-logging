// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Serilog.Framework.Logging;
using Serilog;

namespace Microsoft.Framework.Logging
{
    public static class SerilogLoggerFactoryExtensions
    {
        public static ILoggerFactory AddSerilog(
            this ILoggerFactory factory,
            LoggerConfiguration loggerConfiguration)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            if (loggerConfiguration == null) throw new ArgumentNullException("loggerConfiguration");

            factory.AddProvider(new SerilogLoggerProvider(loggerConfiguration));

            return factory;
        }
    }
}