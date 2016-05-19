// Copyright 2016 Serilog Contributors
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

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Extensions.Logging
{
    /// <summary>
    /// Attaches <see cref="EventId"/> properties to events that have them. Uses a more compact format than
    /// the default destructuring provides (no type tag, default properties omitted).
    /// </summary>
    class EventIdEnricher : ILogEventEnricher
    {
        readonly EventId _eventId;

        public EventIdEnricher(EventId eventId)
        {
            _eventId = eventId;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var properties = new List<LogEventProperty>(2);

            if (_eventId.Id != 0)
            {
                properties.Add(new LogEventProperty("Id", new ScalarValue(_eventId.Id)));
            }

            if (_eventId.Name != null)
            {
                properties.Add(new LogEventProperty("Name", new ScalarValue(_eventId.Name)));
            }

            logEvent.AddOrUpdateProperty(new LogEventProperty("EventId", new StructureValue(properties)));
        }
    }
}