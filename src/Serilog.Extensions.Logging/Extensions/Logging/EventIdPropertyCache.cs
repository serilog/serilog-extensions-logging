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

namespace Serilog.Extensions.Logging
{
    using Microsoft.Extensions.Logging;
    using Serilog.Events;

    internal sealed class EventIdPropertyCache
    {
        private readonly object _createLock = new();
        private readonly int _maxCapacity;
        private readonly Dictionary<EventKey, LogEventProperty> _propertyCache;

        private int count;

        public EventIdPropertyCache(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
            _propertyCache = new Dictionary<EventKey, LogEventProperty>(capacity: maxCapacity);
        }

        public LogEventProperty GetOrCreateProperty(in EventId eventId)
        {
            var eventKey = new EventKey(eventId);

            if (_propertyCache.TryGetValue(eventKey, out var cachedProperty))
            {
                return cachedProperty;
            }

            lock (_createLock)
            {
                // Double check under lock
                if (_propertyCache.TryGetValue(eventKey, out cachedProperty))
                {
                    return cachedProperty;
                }

                cachedProperty = CreateCore(in eventKey);

                if (count < _maxCapacity)
                {
                    _propertyCache[eventKey] = cachedProperty;
                    count++;
                }

                return cachedProperty;
            }
        }

        private static LogEventProperty CreateCore(in EventKey eventKey)
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

            return new LogEventProperty("EventId", new StructureValue(properties));
        }

        private readonly record struct EventKey
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
}
