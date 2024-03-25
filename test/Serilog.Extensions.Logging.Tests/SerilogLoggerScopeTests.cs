// Copyright © Serilog Contributors
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Serilog.Events;
using Serilog.Extensions.Logging.Tests.Support;

using Xunit;

namespace Serilog.Extensions.Logging.Tests;

public class SerilogLoggerScopeTests
{
    static (SerilogLoggerProvider, LogEventPropertyFactory, LogEvent) SetUp()
    {
        var loggerProvider = new SerilogLoggerProvider();

        var logEventPropertyFactory = new LogEventPropertyFactory();

        var dateTimeOffset = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var messageTemplate = new MessageTemplate([]);
        var properties = Enumerable.Empty<LogEventProperty>();
        var logEvent = new LogEvent(dateTimeOffset, LogEventLevel.Information, null, messageTemplate, properties);

        return (loggerProvider, logEventPropertyFactory, logEvent);
    }

    [Fact]
    public void EnrichWithDictionaryStringObject()
    {
        const string propertyName = "Foo";
        const string expectedValue = "Bar";

        var(loggerProvider, logEventPropertyFactory, logEvent) = SetUp();

        var state = new Dictionary<string, object?>() { { propertyName, expectedValue } };

        var loggerScope = new SerilogLoggerScope(loggerProvider, state);

        loggerScope.EnrichAndCreateScopeItem(logEvent, logEventPropertyFactory, out LogEventPropertyValue? _);

        Assert.Contains(propertyName, logEvent.Properties);

        var scalarValue = logEvent.Properties[propertyName] as ScalarValue;
        Assert.NotNull(scalarValue);

        var actualValue = scalarValue.Value as string;
        Assert.NotNull(actualValue);
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void EnrichWithIEnumerableKeyValuePairStringObject()
    {
        const string propertyName = "Foo";
        const string expectedValue = "Bar";

        var (loggerProvider, logEventPropertyFactory, logEvent) = SetUp();

        var state = new KeyValuePair<string, object?>[] { new(propertyName, expectedValue) };

        var loggerScope = new SerilogLoggerScope(loggerProvider, state);

        loggerScope.EnrichAndCreateScopeItem(logEvent, logEventPropertyFactory, out LogEventPropertyValue? _);

        Assert.Contains(propertyName, logEvent.Properties);

        var scalarValue = logEvent.Properties[propertyName] as ScalarValue;
        Assert.NotNull(scalarValue);

        var actualValue = scalarValue.Value as string;
        Assert.NotNull(actualValue);
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void EnrichWithTupleStringObject()
    {
        const string propertyName = "Foo";
        const string expectedValue = "Bar";

        var (loggerProvider, logEventPropertyFactory, logEvent) = SetUp();

        var state = (propertyName, (object)expectedValue);

        var loggerScope = new SerilogLoggerScope(loggerProvider, state);

        loggerScope.EnrichAndCreateScopeItem(logEvent, logEventPropertyFactory, out LogEventPropertyValue? _);

        Assert.Contains(propertyName, logEvent.Properties);

        var scalarValue = logEvent.Properties[propertyName] as ScalarValue;
        Assert.NotNull(scalarValue);

        var actualValue = scalarValue.Value as string;
        Assert.NotNull(actualValue);
        Assert.Equal(expectedValue, actualValue);
    }
}
