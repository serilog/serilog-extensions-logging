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
        private readonly Dictionary<int, LogEventProperty> _propertyCache;

        private int count;

        public EventIdPropertyCache(int maxCapacity)
        {
            this._maxCapacity = maxCapacity;
            this._propertyCache = new Dictionary<int, LogEventProperty>(capacity: maxCapacity);
        }

        public LogEventProperty GetOrCreateProperty(in EventId eventId)
        {
            if (_propertyCache.TryGetValue(eventId.Id, out var cachedProperty))
            {
                return cachedProperty;
            }

            lock (_createLock)
            {
                return GetOrCreateSynchronized(in eventId);
            }
        }

        private static LogEventProperty CreateCore(in EventId eventId)
        {
            var properties = new List<LogEventProperty>(2);

            if (eventId.Id != 0)
            {
                properties.Add(new LogEventProperty("Id", new ScalarValue(eventId.Id)));
            }

            if (eventId.Name != null)
            {
                properties.Add(new LogEventProperty("Name", new ScalarValue(eventId.Name)));
            }

            return new LogEventProperty("EventId", new StructureValue(properties));
        }

        private LogEventProperty GetOrCreateSynchronized(in EventId eventId)
        {
            // Double check under lock
            if (_propertyCache.TryGetValue(eventId.Id, out var cachedProperty))
            {
                return cachedProperty;
            }

            cachedProperty = CreateCore(in eventId);

            if (count < _maxCapacity)
            {
                _propertyCache[eventId.Id] = cachedProperty;
                count++;
            }
            
            return cachedProperty;
        }
    }
}
