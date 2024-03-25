// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Serilog.Core;
using Serilog.Events;

namespace Serilog.Extensions.Logging.Tests.Support;

public class SerilogSink : ILogEventSink
{
    public List<LogEvent> Writes { get; set; } = [];

    public void Emit(LogEvent logEvent)
    {
        Writes.Add(logEvent);
    }
}
