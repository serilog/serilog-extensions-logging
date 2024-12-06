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

using Microsoft.Extensions.Logging;
using Serilog.Events;
using Xunit;

namespace Serilog.Extensions.Logging.Tests;

public class EventIdPropertyCacheTests
{
    [Fact]
    public void CreatesPropertyValueWithCorrectIdAndName()
    {
        // Arrange
        const int id = 101;
        const string name = "TestEvent";
        var eventId = new EventId(id, name);

        var cache = new EventIdPropertyCache();

        // Act
        var eventPropertyValue = cache.GetOrCreatePropertyValue(eventId);

        // Assert
        var value = Assert.IsType<StructureValue>(eventPropertyValue);

        Assert.Equal(2, value.Properties.Count);

        var idValue = value.Properties.Single(property => property.Name == "Id").Value;
        var nameValue = value.Properties.Single(property => property.Name == "Name").Value;

        var scalarId = Assert.IsType<ScalarValue>(idValue);
        var scalarName = Assert.IsType<ScalarValue>(nameValue);

        Assert.Equal(id, scalarId.Value);
        Assert.Equal(name, scalarName.Value);
    }

    [Fact]
    public void EventsWithDSameKeysHaveSameReferences()
    {
        // Arrange
        var cache = new EventIdPropertyCache();

        // Act
        var propertyValue1 = cache.GetOrCreatePropertyValue(new EventId(1, "Name1"));
        var propertyValue2 = cache.GetOrCreatePropertyValue(new EventId(1, "Name1"));

        // Assert
        Assert.Same(propertyValue1, propertyValue2);
    }

    [Theory]
    [InlineData(1, "SomeName", 1, "AnotherName")]
    [InlineData(1, "SomeName", 2, "SomeName")]
    [InlineData(1, "SomeName", 2, "AnotherName")]
    public void EventsWithDifferentKeysHaveDifferentReferences(int firstId, string firstName, int secondId, string secondName)
    {
        // Arrange
        var cache = new EventIdPropertyCache();

        // Act
        var propertyValue1 = cache.GetOrCreatePropertyValue(new EventId(firstId, firstName));
        var propertyValue2 = cache.GetOrCreatePropertyValue(new EventId(secondId, secondName));

        // Assert
        Assert.NotSame(propertyValue1, propertyValue2);
    }


    [Fact]
    public void WhenLimitIsNotOverSameEventsHaveSameReferences()
    {
        // Arrange
        var eventId = new EventId(101, "test");
        var cache = new EventIdPropertyCache();

        // Act
        var propertyValue1 = cache.GetOrCreatePropertyValue(eventId);
        var propertyValue2 = cache.GetOrCreatePropertyValue(eventId);

        // Assert
        Assert.Same(propertyValue1, propertyValue2);
    }

    [Fact]
    public void WhenLimitIsOverSameEventsHaveDifferentReferences()
    {
        // Arrange
        var cache = new EventIdPropertyCache(maxCachedProperties: 1);
        cache.GetOrCreatePropertyValue(new EventId(1, "InitialEvent"));

        var eventId = new EventId(101, "DifferentEvent");

        // Act
        var propertyValue1 = cache.GetOrCreatePropertyValue(eventId);
        var propertyValue2 = cache.GetOrCreatePropertyValue(eventId);

        // Assert
        Assert.NotSame(propertyValue1, propertyValue2);
    }
}
