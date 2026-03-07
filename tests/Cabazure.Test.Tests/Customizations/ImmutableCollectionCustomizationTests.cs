using System.Collections.Immutable;
using AutoFixture;
using Cabazure.Test.Customizations;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Customizations;

public class ImmutableCollectionCustomizationTests
{
    private sealed class HasImmutableProperty
    {
        public ImmutableList<string> Tags { get; set; } = ImmutableList<string>.Empty;
    }

    [Fact]
    public void Customize_WithNullFixture_Throws()
    {
        var sut = new ImmutableCollectionCustomization();

        var act = () => sut.Customize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ImmutableArray_ReturnsPopulatedInstance()
    {
        var fixture = FixtureFactory.Create(new ImmutableCollectionCustomization());

        var result = fixture.Create<ImmutableArray<string>>();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ImmutableList_ReturnsPopulatedInstance()
    {
        var fixture = FixtureFactory.Create(new ImmutableCollectionCustomization());

        var result = fixture.Create<ImmutableList<string>>();

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_ImmutableHashSet_ReturnsPopulatedInstance()
    {
        var fixture = FixtureFactory.Create(new ImmutableCollectionCustomization());

        var result = fixture.Create<ImmutableHashSet<string>>();

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_ImmutableSortedSet_ReturnsPopulatedInstance()
    {
        var fixture = FixtureFactory.Create(new ImmutableCollectionCustomization());

        var result = fixture.Create<ImmutableSortedSet<string>>();

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_ImmutableDictionary_ReturnsPopulatedInstance()
    {
        var fixture = FixtureFactory.Create(new ImmutableCollectionCustomization());

        var result = fixture.Create<ImmutableDictionary<string, int>>();

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_ImmutableSortedDictionary_ReturnsPopulatedInstance()
    {
        var fixture = FixtureFactory.Create(new ImmutableCollectionCustomization());

        var result = fixture.Create<ImmutableSortedDictionary<string, int>>();

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_ImmutableQueue_ReturnsPopulatedInstance()
    {
        var fixture = FixtureFactory.Create(new ImmutableCollectionCustomization());

        var result = fixture.Create<ImmutableQueue<string>>();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ImmutableStack_ReturnsPopulatedInstance()
    {
        var fixture = FixtureFactory.Create(new ImmutableCollectionCustomization());

        var result = fixture.Create<ImmutableStack<string>>();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public void Customize_PopulatesProperty_OnObjectWithImmutableListProperty()
    {
        var fixture = FixtureFactory.Create(new ImmutableCollectionCustomization());

        var result = fixture.Create<HasImmutableProperty>();

        result.Tags.Should().NotBeEmpty();
    }
}
