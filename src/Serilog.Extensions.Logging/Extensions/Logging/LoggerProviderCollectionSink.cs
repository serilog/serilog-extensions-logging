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

using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Extensions.Logging
{
    class LoggerProviderCollectionSink : ILogEventSink, IDisposable
    {
        readonly LoggerProviderCollection _providers;

        public LoggerProviderCollectionSink(LoggerProviderCollection providers)
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        }

        public void Emit(LogEvent logEvent)
        {
            string categoryName = null;

            if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextProperty) &&
                sourceContextProperty is ScalarValue sourceContextValue &&
                sourceContextValue.Value is string sourceContext)
            {
                categoryName = sourceContext;
            }

            // Allocates like mad, but first make it work, then make it work fast ;-)
            var flv = new FormattedLogValues(
                logEvent.MessageTemplate.Text, 
                logEvent.MessageTemplate.Tokens
                    .OfType<PropertyToken>()
                    .Select(p =>
                    {
                        if (!logEvent.Properties.TryGetValue(p.PropertyName, out var value))
                            return null;
                        if (value is ScalarValue sv)
                            return sv.Value;
                        return value;
                    })
                    .ToArray());

            foreach (var provider in _providers.Providers)
            {
                var logger = provider.CreateLogger(categoryName);

                logger.Log(
                    LevelMapping.ToExtensionsLevel(logEvent.Level), 
                    default(EventId), 
                    flv,
                    logEvent.Exception,
                    (s, e) => s.ToString());
            }
        }

        public void Dispose()
        {
            _providers.Dispose();
        }
    }
}
