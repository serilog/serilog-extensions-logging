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

using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Serilog;

/// <summary>
/// Extensions for <see cref="LoggerSinkConfiguration"/>.
/// </summary>
public static class LoggerSinkConfigurationExtensions
{
    /// <summary>
    /// Write Serilog events to the providers in <paramref name="providers"/>.
    /// </summary>
    /// <param name="configuration">The `WriteTo` object.</param>
    /// <param name="providers">A <see cref="LoggerProviderCollection"/> to write events to.</param>
    /// <param name="restrictedToMinimumLevel">The minimum level for
    /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
    /// <param name="levelSwitch">A switch allowing the pass-through minimum level
    /// to be changed at runtime.</param>
    /// <returns>A <see cref="LoggerConfiguration"/> to allow method chaining.</returns>
    public static LoggerConfiguration Providers(
        this LoggerSinkConfiguration configuration,
        LoggerProviderCollection providers,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (providers == null) throw new ArgumentNullException(nameof(providers));
        return configuration.Sink(new LoggerProviderCollectionSink(providers), restrictedToMinimumLevel, levelSwitch);
    }
}
