using System.Text.Json;
using Cabazure.Test;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Assertions;

public class JsonElementEquivalencyStepTests
{
    [Fact]
    public void UsingJsonElementComparison_WithIdenticalDtoJsonElements_Passes()
    {
        var json = """{"value":1}""";
        var dto1 = new TestDto { Name = "A", Data = JsonDocument.Parse(json).RootElement };
        var dto2 = new TestDto { Name = "A", Data = JsonDocument.Parse(json).RootElement };

        var act = () => dto1.Should().BeEquivalentTo(dto2, opts => opts.UsingJsonElementComparison());

        act.Should().NotThrow();
    }

    [Fact]
    public void UsingJsonElementComparison_WithSemanticallyEqualButDifferentlyFormattedJson_Passes()
    {
        var dto1 = new TestDto { Name = "A", Data = JsonDocument.Parse("""{"value":1}""").RootElement };
        var dto2 = new TestDto { Name = "A", Data = JsonDocument.Parse("""{ "value": 1 }""").RootElement };

        var act = () => dto1.Should().BeEquivalentTo(dto2, opts => opts.UsingJsonElementComparison());

        act.Should().NotThrow();
    }

    [Fact]
    public void UsingJsonElementComparison_WithDifferentJsonValues_Throws()
    {
        var dto1 = new TestDto { Name = "A", Data = JsonDocument.Parse("""{"value":1}""").RootElement };
        var dto2 = new TestDto { Name = "A", Data = JsonDocument.Parse("""{"value":2}""").RootElement };

        Action act = () => dto1.Should().BeEquivalentTo(dto2, opts => opts.UsingJsonElementComparison());

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void BeEquivalentTo_WithoutUsingJsonElementComparison_AndDifferentlyFormattedJson_Throws()
    {
        // Documents the need for UsingJsonElementComparison():
        // without it, semantically equal but differently-formatted JsonElement values fail structural comparison.
        var dto1 = new TestDto { Name = "A", Data = JsonDocument.Parse("""{"value":1}""").RootElement };
        var dto2 = new TestDto { Name = "A", Data = JsonDocument.Parse("""{ "value": 1 }""").RootElement };

        Action act = () => dto1.Should().BeEquivalentTo(dto2);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void UsingJsonElementComparison_WhenJsonDiffers_FailureMessageContainsBothJsonStrings()
    {
        var dto1 = new TestDto { Name = "A", Data = JsonDocument.Parse("""{"value":1}""").RootElement };
        var dto2 = new TestDto { Name = "A", Data = JsonDocument.Parse("""{"value":2}""").RootElement };

        Action act = () => dto1.Should().BeEquivalentTo(dto2, opts => opts.UsingJsonElementComparison());

        var exception = act.Should().Throw<Exception>();
        exception.Which.Message.Should().Contain("{\"value\":1}");
        exception.Which.Message.Should().Contain("{\"value\":2}");
    }

    private sealed class TestDto
    {
        public string Name { get; set; } = string.Empty;
        public JsonElement Data { get; set; }
    }
}
