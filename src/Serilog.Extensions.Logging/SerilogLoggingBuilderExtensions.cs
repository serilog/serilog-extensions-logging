// Copyright 2017 Serilog Contributors
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;

namespace Serilog;

/// <summary>
/// Extends <see cref="ILoggingBuilder"/> with Serilog configuration methods.
/// </summary>
public static class SerilogLoggingBuilderExtensions
{
    /// <summary>
    /// Add Serilog to the logging pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Logging.ILoggingBuilder" /> to add logging provider to.</param>
    /// <param name="logger">The Serilog logger; if not supplied, the static <see cref="Serilog.Log"/> will be used.</param>
    /// <param name="dispose">When true, dispose <paramref name="logger"/> when the framework disposes the provider. If the
    /// logger is not specified but <paramref name="dispose"/> is true, the <see cref="Log.CloseAndFlush()"/> method will be
    /// called on the static <see cref="Log"/> class instead.</param>
    /// <returns>Reference to the supplied <paramref name="builder"/>.</returns>
    public static ILoggingBuilder AddSerilog(this ILoggingBuilder builder, ILogger? logger = null, bool dispose = false)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        if (dispose)
        {
            builder.Services.AddSingleton<ILoggerProvider, SerilogLoggerProvider>(services => new SerilogLoggerProvider(logger, true));
        }
        else
        {
            builder.AddProvider(new SerilogLoggerProvider(logger));
        }

        builder.AddFilter<SerilogLoggerProvider>(null, LogLevel.Trace);

        return builder;
    }
}
