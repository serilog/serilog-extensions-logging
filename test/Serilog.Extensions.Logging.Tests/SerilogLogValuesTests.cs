using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Serilog.Extensions.Logging.Tests;

public class SerilogLogValuesTests
{
    [Fact]
    public void OriginalFormatIsExposed()
    {
        const string format = "Hello, {Name}!";
        var mt = new MessageTemplateParser().Parse(format);
        var lv = new SerilogLogValues(mt, new Dictionary<string, LogEventPropertyValue>());
        var kvp = lv.Single();
        Assert.Equal("{OriginalFormat}", kvp.Key);
        Assert.Equal(format, kvp.Value);
    }

    [Fact]
    public void ScalarPropertiesAreSimplified()
    {
        const string name = "Scalar";
        var scalar = 15;
        var lv = new SerilogLogValues(MessageTemplate.Empty, new Dictionary<string, LogEventPropertyValue> { [name] = new ScalarValue(scalar) });
        var kvp = lv.Single(p => p.Key == name);
        var sv = Assert.IsType<int>(kvp.Value);
        Assert.Equal(scalar, sv);
    }

    [Fact]
    public void NonScalarPropertiesAreWrapped()
    {
        const string name = "Sequence";
        var seq = new SequenceValue([]);
        var lv = new SerilogLogValues(MessageTemplate.Empty, new Dictionary<string, LogEventPropertyValue> { [name] = seq });
        var kvp = lv.Single(p => p.Key == name);
        var sv = Assert.IsType<SequenceValue>(kvp.Value);
        Assert.Equal(seq, sv);
    }

    [Fact]
    public void MessageTemplatesAreRendered()
    {
        const string format = "Hello, {Name}!";
        var mt = new MessageTemplateParser().Parse(format);
        var lv = new SerilogLogValues(mt, new Dictionary<string, LogEventPropertyValue> { ["Name"] = new ScalarValue("World") });
        Assert.Equal("Hello, \"World\"!", lv.ToString());
    }
}
