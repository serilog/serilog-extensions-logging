// Copyright (c) Serilog Contributors
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

using Serilog.Events;
using Serilog.Parsing;
using System.Collections;

namespace Serilog.Extensions.Logging;

sealed class CachingMessageTemplateParser
{
    readonly MessageTemplateParser _innerParser = new();

    readonly object _templatesLock = new();
    readonly Hashtable _templates = new();

    const int MaxCacheItems = 1000;
    const int MaxCachedTemplateLength = 1024;

    public MessageTemplate Parse(string messageTemplate)
    {
        if (messageTemplate == null) throw new ArgumentNullException(nameof(messageTemplate));

        if (messageTemplate.Length > MaxCachedTemplateLength)
            return _innerParser.Parse(messageTemplate);

        // ReSharper disable once InconsistentlySynchronizedField
        // ignored warning because this is by design
        var result = (MessageTemplate?)_templates[messageTemplate];
        if (result != null)
            return result;

        result = _innerParser.Parse(messageTemplate);

        lock (_templatesLock)
        {
            // Exceeding MaxCacheItems is *not* the sunny day scenario; all we're doing here is preventing out-of-memory
            // conditions when the library is used incorrectly. Correct use (templates, rather than
            // direct message strings) should barely, if ever, overflow this cache.

            // Changing workloads through the lifecycle of an app instance mean we can gain some ground by
            // potentially dropping templates generated only in startup, or only during specific infrequent
            // activities.

            if (_templates.Count == MaxCacheItems)
                _templates.Clear();

            _templates[messageTemplate] = result;
        }

        return result;
    }
}
