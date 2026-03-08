using System.Text.Json;
using AutoFixture;
using Cabazure.Test.Customizations;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Customizations;

public class JsonElementCustomizationTests
{
    private sealed class HasJsonElementProperty
    {
        public JsonElement Payload { get; set; }
    }

    [Fact]
    public void Customize_WithNullFixture_Throws()
    {
        var sut = new JsonElementCustomization();

        var act = () => sut.Customize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_JsonElement_ReturnsStringElement()
    {
        var fixture = FixtureFactory.Create(new JsonElementCustomization());

        var result = fixture.Create<JsonElement>();

        result.ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void Create_JsonElement_HasNonEmptyStringValue()
    {
        var fixture = FixtureFactory.Create(new JsonElementCustomization());

        var result = fixture.Create<JsonElement>();

        result.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_JsonElement_IsClonedAndStandalone()
    {
        var fixture = FixtureFactory.Create(new JsonElementCustomization());

        var result = fixture.Create<JsonElement>();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        result.ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void CanBeUsed_WithFixtureFactory()
    {
        var fixture = FixtureFactory.Create(new JsonElementCustomization());

        var result = fixture.Create<JsonElement>();

        result.ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void Create_JsonElement_AsPropertyOnObject()
    {
        var fixture = FixtureFactory.Create(new JsonElementCustomization());

        var result = fixture.Create<HasJsonElementProperty>();

        result.Payload.ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void Create_JsonElement_WithCustomFactory_ReturnsCustomElement()
    {
        var fixture = FixtureFactory.Create(
            new JsonElementCustomization(_ => "42"));

        var result = fixture.Create<JsonElement>();

        result.ValueKind.Should().Be(JsonValueKind.Number);
        result.GetInt32().Should().Be(42);
    }

    [Fact]
    public void Create_JsonElement_WithCustomFactory_UsesFixture()
    {
        var fixture = FixtureFactory.Create(
            new JsonElementCustomization(
                f => $"{{\"key\":\"{f.Create<string>()}\"}}"));

        var result = fixture.Create<JsonElement>();

        result.ValueKind.Should().Be(JsonValueKind.Object);
        result.EnumerateObject().Should().NotBeEmpty();
    }

    [Fact]
    public void Create_JsonElement_WithElementFactory_ReturnsElement()
    {
        var expected = JsonDocument.Parse("true").RootElement.Clone();
        var fixture = FixtureFactory.Create(
            new JsonElementCustomization(_ => expected));

        var result = fixture.Create<JsonElement>();

        result.ValueKind.Should().Be(JsonValueKind.True);
    }
}
