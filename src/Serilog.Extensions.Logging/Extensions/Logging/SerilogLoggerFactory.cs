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
using Serilog.Debugging;

namespace Serilog.Extensions.Logging;

/// <summary>
/// A complete Serilog-backed implementation of the .NET Core logging infrastructure.
/// </summary>
public class SerilogLoggerFactory : ILoggerFactory
{
    readonly LoggerProviderCollection? _providerCollection;
    readonly SerilogLoggerProvider _provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerilogLoggerFactory"/> class.
    /// </summary>
    /// <param name="logger">The Serilog logger; if not supplied, the static <see cref="Serilog.Log"/> will be used.</param>
    /// <param name="dispose">When true, dispose <paramref name="logger"/> when the framework disposes the provider. If the
    /// logger is not specified but <paramref name="dispose"/> is true, the <see cref="Log.CloseAndFlush()"/> method will be
    /// called on the static <see cref="Log"/> class instead.</param>
    /// <param name="providerCollection">A <see cref="LoggerProviderCollection"/>, for use with <c>WriteTo.Providers()</c>.</param>
    public SerilogLoggerFactory(ILogger? logger = null, bool dispose = false, LoggerProviderCollection? providerCollection = null)
    {
        _provider = new SerilogLoggerProvider(logger, dispose);
        _providerCollection = providerCollection;
    }

    /// <summary>
    /// Disposes the provider.
    /// </summary>
    public void Dispose()
    {
        _provider.Dispose();
    }

    /// <summary>
    /// Creates a new <see cref="T:Microsoft.Extensions.Logging.ILogger" /> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>
    /// The <see cref="T:Microsoft.Extensions.Logging.ILogger" />.
    /// </returns>
    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return _provider.CreateLogger(categoryName);
    }

    /// <summary>
    /// Adds an <see cref="T:Microsoft.Extensions.Logging.ILoggerProvider" /> to the logging system.
    /// </summary>
    /// <param name="provider">The <see cref="T:Microsoft.Extensions.Logging.ILoggerProvider" />.</param>
    public void AddProvider(ILoggerProvider provider)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        if (_providerCollection != null)
            _providerCollection.AddProvider(provider);
        else
            SelfLog.WriteLine("Ignoring added logger provider {0}", provider);
    }
}
