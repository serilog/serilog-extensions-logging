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

namespace Serilog.Extensions.Logging
{
    /// <summary>
    /// Converts between Serilog and Microsoft.Extensions.Logging level enum values.
    /// </summary>
    public static class LevelConvert
    {
        /// <summary>
        /// Convert <paramref name="logLevel"/> to the equivalent Serilog <see cref="LogEventLevel"/>.
        /// </summary>
        /// <param name="logLevel">A Microsoft.Extensions.Logging <see cref="LogLevel"/>.</param>
        /// <returns>The Serilog equivalent of <paramref name="logLevel"/>.</returns>
        /// <remarks>The <see cref="LogLevel.None"/> value has no Serilog equivalent. It is mapped to
        /// <see cref="LogEventLevel.Fatal"/> as the closest approximation, but this has entirely
        /// different semantics.</remarks>
        public static LogEventLevel ToSerilogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.None:
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                case LogLevel.Trace:
                default:
                    return LogEventLevel.Verbose;
            }
        }

        /// <summary>
        /// Convert <paramref name="logEventLevel"/> to the equivalent Microsoft.Extensions.Logging <see cref="LogLevel"/>.
        /// </summary>
        /// <param name="logEventLevel">A Serilog <see cref="LogEventLevel"/>.</param>
        /// <returns>The Microsoft.Extensions.Logging equivalent of <paramref name="logEventLevel"/>.</returns>
        public static LogLevel ToExtensionsLevel(LogEventLevel logEventLevel)
        {
            switch (logEventLevel)
            {
                case LogEventLevel.Fatal:
                    return LogLevel.Critical;
                case LogEventLevel.Error:
                    return LogLevel.Error;
                case LogEventLevel.Warning:
                    return LogLevel.Warning;
                case LogEventLevel.Information:
                    return LogLevel.Information;
                case LogEventLevel.Debug:
                    return LogLevel.Debug;
                case LogEventLevel.Verbose:
                default:
                    return LogLevel.Trace;
            }
        }
    }
}
