// Copyright © Serilog Contributors
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Serilog.Core;
using Serilog.Events;

namespace Serilog.Extensions.Logging.Tests.Support;

internal class LogEventPropertyFactory : ILogEventPropertyFactory
{
    public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
    {
        var scalarValue = new ScalarValue(value);
        return new LogEventProperty(name, scalarValue);
    }
}
