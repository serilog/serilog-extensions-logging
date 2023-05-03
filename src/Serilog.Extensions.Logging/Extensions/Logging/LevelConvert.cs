// Copyright 2019 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Serilog.Events;

// ReSharper disable RedundantCaseLabel

namespace Serilog.Extensions.Logging;

/// <summary>
/// Converts between Serilog and Microsoft.Extensions.Logging level enum values.
/// </summary>
public static class LevelConvert
{
    /// <summary>
    /// Convert <paramref name="logLevel"/> to the equivalent Serilog <see cref="LogEventLevel"/>.
    /// </summary>
    /// <param name="logLevel">A Microsoft.Extensions.Logging <see cref="Microsoft.Extensions.Logging.LogLevel"/>.</param>
    /// <returns>The Serilog equivalent of <paramref name="logLevel"/>.</returns>
    /// <remarks>The <see cref="Microsoft.Extensions.Logging.LogLevel.None"/> value has no Serilog equivalent. It is mapped to
    /// <see cref="LogEventLevel.Fatal"/> as the closest approximation, but this has entirely
    /// different semantics.</remarks>
    public static LogEventLevel ToSerilogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.None or LogLevel.Critical => LogEventLevel.Fatal,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Verbose,
        };
    }

    /// <summary>
    /// Convert <paramref name="logEventLevel"/> to the equivalent Microsoft.Extensions.Logging <see cref="Microsoft.Extensions.Logging.LogLevel"/>.
    /// </summary>
    /// <param name="logEventLevel">A Serilog <see cref="LogEventLevel"/>.</param>
    /// <returns>The Microsoft.Extensions.Logging equivalent of <paramref name="logEventLevel"/>.</returns>
    public static LogLevel ToExtensionsLevel(LogEventLevel logEventLevel)
    {
        return logEventLevel switch
        {
            LogEventLevel.Fatal => LogLevel.Critical,
            LogEventLevel.Error => LogLevel.Error,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Debug => LogLevel.Debug,
            _ => LogLevel.Trace,
        };
    }
}
