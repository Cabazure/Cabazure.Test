using System.Text.Json;
using System.Xml;
using Cabazure.Test;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Assertions;

public class StringContentExtensionsTests
{
    // BeSimilarTo

    [Fact]
    public void BeSimilarTo_WithIdenticalContent_Passes()
    {
        var subject = "hello world";

        var act = () => subject.Should().BeSimilarTo("hello world");

        act.Should().NotThrow();
    }

    [Fact]
    public void BeSimilarTo_WithSameContentDifferentWhitespace_Passes()
    {
        var subject = "hello  \n  world";

        var act = () => subject.Should().BeSimilarTo("hello world");

        act.Should().NotThrow();
    }

    [Fact]
    public void BeSimilarTo_WithLeadingAndTrailingWhitespace_Passes()
    {
        var subject = "  hello  ";

        var act = () => subject.Should().BeSimilarTo("hello");

        act.Should().NotThrow();
    }

    [Fact]
    public void BeSimilarTo_WithDifferentLineEndings_Passes()
    {
        var subject = "line1\nline2";

        var act = () => subject.Should().BeSimilarTo("line1 line2");

        act.Should().NotThrow();
    }

    [Fact]
    public void BeSimilarTo_WithDifferentContent_ThrowsWithMessage()
    {
        var subject = "hello world";

        var act = () => subject.Should().BeSimilarTo("goodbye world");

        var exception = act.Should().Throw<Exception>();
        exception.Which.Message.Should().Contain("hello world");
        exception.Which.Message.Should().Contain("goodbye world");
    }

    [Fact]
    public void NotBeSimilarTo_WithDifferentContent_Passes()
    {
        var subject = "hello world";

        var act = () => subject.Should().NotBeSimilarTo("goodbye world");

        act.Should().NotThrow();
    }

    [Fact]
    public void NotBeSimilarTo_WithSameContentDifferentWhitespace_Throws()
    {
        var subject = "hello  \n  world";

        var act = () => subject.Should().NotBeSimilarTo("hello world");

        act.Should().Throw<Exception>();
    }

    // BeXmlEquivalentTo

    [Fact]
    public void BeXmlEquivalentTo_WithIdenticalXml_Passes()
    {
        var subject = "<root><child /></root>";

        var act = () => subject.Should().BeXmlEquivalentTo("<root><child /></root>");

        act.Should().NotThrow();
    }

    [Fact]
    public void BeXmlEquivalentTo_WithSameXmlDifferentFormatting_Passes()
    {
        var indented = """
            <root>
              <child>value</child>
            </root>
            """;
        var compact = "<root><child>value</child></root>";

        var act = () => indented.Should().BeXmlEquivalentTo(compact);

        act.Should().NotThrow();
    }

    [Fact]
    public void BeXmlEquivalentTo_WithDifferentXmlContent_ThrowsWithMessage()
    {
        var subject = "<root><child>hello</child></root>";
        var expected = "<root><child>goodbye</child></root>";

        var act = () => subject.Should().BeXmlEquivalentTo(expected);

        var exception = act.Should().Throw<Exception>();
        exception.Which.Message.Should().Contain("hello");
        exception.Which.Message.Should().Contain("goodbye");
    }

    [Fact]
    public void BeXmlEquivalentTo_WithInvalidXml_ThrowsXmlException()
    {
        var subject = "<root><child /></root>";
        var invalidXml = "<root><unclosed>";

        var act = () => subject.Should().BeXmlEquivalentTo(invalidXml);

        act.Should().Throw<XmlException>();
    }

    [Fact]
    public void NotBeXmlEquivalentTo_WithDifferentXmlContent_Passes()
    {
        var subject = "<root><child>hello</child></root>";
        var other = "<root><child>goodbye</child></root>";

        var act = () => subject.Should().NotBeXmlEquivalentTo(other);

        act.Should().NotThrow();
    }

    [Fact]
    public void NotBeXmlEquivalentTo_WithSameXmlDifferentFormatting_Throws()
    {
        var indented = """
            <root>
              <child>value</child>
            </root>
            """;
        var compact = "<root><child>value</child></root>";

        var act = () => indented.Should().NotBeXmlEquivalentTo(compact);

        act.Should().Throw<Exception>();
    }

    // BeJsonEquivalentTo

    [Fact]
    public void BeJsonEquivalentTo_WithIdenticalJson_Passes()
    {
        var subject = """{"name":"Alice","age":30}""";

        var act = () => subject.Should().BeJsonEquivalentTo("""{"name":"Alice","age":30}""");

        act.Should().NotThrow();
    }

    [Fact]
    public void BeJsonEquivalentTo_WithSameJsonDifferentFormatting_Passes()
    {
        var indented = """
            {
              "name": "Alice",
              "age": 30
            }
            """;
        var compact = """{"name":"Alice","age":30}""";

        var act = () => indented.Should().BeJsonEquivalentTo(compact);

        act.Should().NotThrow();
    }

    [Fact]
    public void BeJsonEquivalentTo_WithDifferentJsonContent_ThrowsWithMessage()
    {
        var subject = """{"name":"Alice","age":30}""";
        var expected = """{"name":"Bob","age":25}""";

        var act = () => subject.Should().BeJsonEquivalentTo(expected);

        var exception = act.Should().Throw<Exception>();
        exception.Which.Message.Should().Contain("Alice");
        exception.Which.Message.Should().Contain("Bob");
    }

    [Fact]
    public void BeJsonEquivalentTo_WithInvalidJson_ThrowsJsonException()
    {
        var subject = """{"name":"Alice","age":30}""";
        var invalidJson = """{"name":"Alice","age":}""";

        var act = () => subject.Should().BeJsonEquivalentTo(invalidJson);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void NotBeJsonEquivalentTo_WithDifferentJsonContent_Passes()
    {
        var subject = """{"name":"Alice","age":30}""";
        var other = """{"name":"Bob","age":25}""";

        var act = () => subject.Should().NotBeJsonEquivalentTo(other);

        act.Should().NotThrow();
    }

    [Fact]
    public void NotBeJsonEquivalentTo_WithSameJsonDifferentFormatting_Throws()
    {
        var indented = """
            {
              "name": "Alice",
              "age": 30
            }
            """;
        var compact = """{"name":"Alice","age":30}""";

        var act = () => indented.Should().NotBeJsonEquivalentTo(compact);

        act.Should().Throw<Exception>();
    }
}
