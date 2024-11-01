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
    using System.Collections.Concurrent;
    using Microsoft.Extensions.Logging;
    using Serilog.Events;

    static class EventIdPropertyCache
    {
        const int MaxCachedProperties = 1024;

        static readonly ConcurrentDictionary<EventKey, LogEventProperty> s_propertyCache = new();
        static int s_count;

        public static LogEventProperty GetOrCreateProperty(in EventId eventId)
        {
            var eventKey = new EventKey(eventId);

            LogEventProperty? property;

            if (s_count >= MaxCachedProperties)
            {
                if (!s_propertyCache.TryGetValue(eventKey, out property))
                {
                    property = CreateCore(in eventKey);
                }
            }
            else
            {
                property = s_propertyCache.GetOrAdd(
                    eventKey,
                    static key =>
                    {
                        Interlocked.Increment(ref s_count);

                        return CreateCore(in key);
                    });
            }

            return property;
        }

        static LogEventProperty CreateCore(in EventKey eventKey)
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
}
