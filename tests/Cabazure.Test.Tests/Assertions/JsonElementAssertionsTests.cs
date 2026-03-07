using System.Text.Json;
using Cabazure.Test;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Assertions;

public class JsonElementAssertionsTests
{
    [Fact]
    public void BeEquivalentTo_WithIdenticalElements_Passes()
    {
        var json = """{"name":"Alice","age":30}""";
        var element1 = JsonDocument.Parse(json).RootElement.Clone();
        var element2 = JsonDocument.Parse(json).RootElement.Clone();

        var act = () => element1.Should().BeEquivalentTo(element2);

        act.Should().NotThrow();
    }

    [Fact]
    public void BeEquivalentTo_WithDifferentValues_ThrowsWithMessage()
    {
        var json1 = """{"name":"Alice","age":30}""";
        var json2 = """{"name":"Bob","age":30}""";
        var element1 = JsonDocument.Parse(json1).RootElement.Clone();
        var element2 = JsonDocument.Parse(json2).RootElement.Clone();

        var act = () => element1.Should().BeEquivalentTo(element2);

        var exception = act.Should().Throw<Exception>();
        exception.Which.Message.Should().Contain("Alice");
        exception.Which.Message.Should().Contain("Bob");
    }

    [Fact]
    public void BeEquivalentTo_WithDifferentStructure_Throws()
    {
        var json1 = """{"a":1}""";
        var json2 = """{"a":1,"b":2}""";
        var element1 = JsonDocument.Parse(json1).RootElement.Clone();
        var element2 = JsonDocument.Parse(json2).RootElement.Clone();

        var act = () => element1.Should().BeEquivalentTo(element2);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void BeEquivalentTo_WithString_MatchingJson_Passes()
    {
        var json = """{"name":"Alice","age":30}""";
        var element = JsonDocument.Parse(json).RootElement.Clone();

        var act = () => element.Should().BeEquivalentTo(json);

        act.Should().NotThrow();
    }

    [Fact]
    public void BeEquivalentTo_WithString_DifferentJson_Throws()
    {
        var json1 = """{"name":"Alice","age":30}""";
        var json2 = """{"name":"Bob","age":25}""";
        var element = JsonDocument.Parse(json1).RootElement.Clone();

        var act = () => element.Should().BeEquivalentTo(json2);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void BeEquivalentTo_WithNullValues_Passes()
    {
        var json = """{"value":null}""";
        var element1 = JsonDocument.Parse(json).RootElement.Clone();
        var element2 = JsonDocument.Parse(json).RootElement.Clone();

        var act = () => element1.Should().BeEquivalentTo(element2);

        act.Should().NotThrow();
    }

    [Fact]
    public void BeEquivalentTo_WithEmptyObjects_Passes()
    {
        var json = """{}""";
        var element1 = JsonDocument.Parse(json).RootElement.Clone();
        var element2 = JsonDocument.Parse(json).RootElement.Clone();

        var act = () => element1.Should().BeEquivalentTo(element2);

        act.Should().NotThrow();
    }

    [Fact]
    public void BeEquivalentTo_WithArrays_OrderSensitive_Passes()
    {
        var json = """[1,2,3]""";
        var element1 = JsonDocument.Parse(json).RootElement.Clone();
        var element2 = JsonDocument.Parse(json).RootElement.Clone();

        var act = () => element1.Should().BeEquivalentTo(element2);

        act.Should().NotThrow();
    }

    [Fact]
    public void BeEquivalentTo_WithArrays_DifferentOrder_Throws()
    {
        var json1 = """[1,2,3]""";
        var json2 = """[3,2,1]""";
        var element1 = JsonDocument.Parse(json1).RootElement.Clone();
        var element2 = JsonDocument.Parse(json2).RootElement.Clone();

        var act = () => element1.Should().BeEquivalentTo(element2);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void BeEquivalentTo_WithString_InvalidJson_ThrowsJsonException()
    {
        var json = """{"name":"Alice","age":30}""";
        var element = JsonDocument.Parse(json).RootElement.Clone();
        var invalidJson = """{"name":"Alice","age":}""";

        var act = () => element.Should().BeEquivalentTo(invalidJson);

        act.Should().Throw<JsonException>();
    }
}
