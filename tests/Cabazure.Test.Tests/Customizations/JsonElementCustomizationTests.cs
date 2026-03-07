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
    public void Create_JsonElement_ReturnsObjectElement()
    {
        var fixture = FixtureFactory.Create(new JsonElementCustomization());

        var result = fixture.Create<JsonElement>();

        result.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void Create_JsonElement_HasAtLeastOneProperty()
    {
        var fixture = FixtureFactory.Create(new JsonElementCustomization());

        var result = fixture.Create<JsonElement>();

        result.EnumerateObject().Should().NotBeEmpty();
    }

    [Fact]
    public void Create_JsonElement_IsClonedAndStandalone()
    {
        var fixture = FixtureFactory.Create(new JsonElementCustomization());

        var result = fixture.Create<JsonElement>();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        result.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void CanBeUsed_WithFixtureFactory()
    {
        var fixture = FixtureFactory.Create(new JsonElementCustomization());

        var result = fixture.Create<JsonElement>();

        result.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void Create_JsonElement_AsPropertyOnObject()
    {
        var fixture = FixtureFactory.Create(new JsonElementCustomization());

        var result = fixture.Create<HasJsonElementProperty>();

        result.Payload.ValueKind.Should().Be(JsonValueKind.Object);
    }
}
