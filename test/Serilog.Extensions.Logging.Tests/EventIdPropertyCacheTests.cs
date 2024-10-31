// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Serilog.Events;
using Xunit;

namespace Serilog.Extensions.Logging.Tests
{
    public class EventIdPropertyCacheTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(48)]
        [InlineData(100)]
        public void LowAndHighNumberedEventIdsAreMapped(int id)
        {
            // Arrange
            var cache = new EventIdPropertyCache(48);

            var eventId = new EventId(id, "test");

            // Act
            var mapped = cache.GetOrCreateProperty(eventId);

            // Assert
            var value = Assert.IsType<StructureValue>(mapped.Value);
            Assert.Equal(2, value.Properties.Count);

            var idValue = value.Properties.Single(p => p.Name == "Id").Value;
            var scalar = Assert.IsType<ScalarValue>(idValue);
            Assert.Equal(id, scalar.Value);
        }
    }
}
