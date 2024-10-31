// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        private readonly struct EventKey : IEquatable<EventKey>
        {
            
            public EventKey(EventId eventId)
            {
                Id = eventId.Id;
                Name = eventId.Name;
            }

            public int Id { get; }

            public string? Name { get; }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = 17;

                    hashCode = (hashCode * 397) ^ this.Id;
                    hashCode = (hashCode * 397) ^ (this.Name?.GetHashCode() ?? 0);

                    return hashCode;
                }
            }

            /// <inheritdoc />
            public bool Equals(EventKey other) => this.Id == other.Id && this.Name == other.Name;

            /// <inheritdoc />
            public override bool Equals(object? obj) => obj is EventKey other && Equals(other);
        }
    }
}
