using AutoFixture;
using Cabazure.Test.Customizations;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Customizations;

public class RecursionCustomizationTests
{
    private class Node
    {
        public Node? Child { get; set; }
    }

    [Fact]
    public void Customize_WithNullFixture_Throws()
    {
        var sut = new RecursionCustomization();

        var act = () => sut.Customize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Customize_RemovesThrowingRecursionBehavior()
    {
        var fixture = new AutoFixture.Fixture();
        var sut = new RecursionCustomization();

        sut.Customize(fixture);

        fixture.Behaviors.OfType<ThrowingRecursionBehavior>().Should().BeEmpty();
    }

    [Fact]
    public void Customize_AddsOmitOnRecursionBehavior()
    {
        var fixture = new AutoFixture.Fixture();
        var sut = new RecursionCustomization();

        sut.Customize(fixture);

        fixture.Behaviors.OfType<OmitOnRecursionBehavior>().Should().ContainSingle();
    }

    [Fact]
    public void Customize_WithRecursiveType_DoesNotThrow()
    {
        var fixture = new AutoFixture.Fixture();
        var sut = new RecursionCustomization();
        sut.Customize(fixture);

        var act = () => fixture.Create<Node>();

        act.Should().NotThrow();
    }

    [Fact]
    public void CanBeUsed_WithFixtureFactory()
    {
        var result = FixtureFactory.Create(new RecursionCustomization());

        result.Should().BeAssignableTo<IFixture>();
    }
}
