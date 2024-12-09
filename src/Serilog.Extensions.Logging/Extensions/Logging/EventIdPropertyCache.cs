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

namespace Serilog.Extensions.Logging;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Events;

sealed class EventIdPropertyCache
{
    readonly int _maxCachedProperties;
    readonly ConcurrentDictionary<EventKey, LogEventPropertyValue> _propertyCache = new();

    int _count;

    public EventIdPropertyCache(int maxCachedProperties = 1024)
    {
        _maxCachedProperties = maxCachedProperties;
    }

    public LogEventPropertyValue GetOrCreatePropertyValue(in EventId eventId)
    {
        var eventKey = new EventKey(eventId);

        LogEventPropertyValue? propertyValue;

        if (_count >= _maxCachedProperties)
        {
            if (!_propertyCache.TryGetValue(eventKey, out propertyValue))
            {
                propertyValue = CreatePropertyValue(in eventKey);
            }
        }
        else
        {
            if (!_propertyCache.TryGetValue(eventKey, out propertyValue))
            {
                // GetOrAdd is moved to a separate method to prevent closure allocation
                propertyValue = GetOrAddCore(in eventKey);
            }
        }

        return propertyValue;
    }

    static LogEventPropertyValue CreatePropertyValue(in EventKey eventKey)
    {
        var properties = new List<LogEventProperty>(2);

        if (eventKey.Id != 0)
        {
            properties.Add(new LogEventProperty("Id", new ScalarValue(eventKey.Id)));
        }

        if (eventKey.Name != null)
        {
            properties.Add(new LogEventProperty("Name", new ScalarValue(eventKey.Name)));
        }

        return new StructureValue(properties);
    }

    LogEventPropertyValue GetOrAddCore(in EventKey eventKey) =>
        _propertyCache.GetOrAdd(
            eventKey,
            key =>
            {
                Interlocked.Increment(ref _count);

                return CreatePropertyValue(in key);
            });

    readonly record struct EventKey
    {
        public EventKey(EventId eventId)
        {
            Id = eventId.Id;
            Name = eventId.Name;
        }

        public int Id { get; }

        public string? Name { get; }
    }
}
