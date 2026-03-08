using AutoFixture.Xunit3;
using FrozenAttribute = AutoFixture.Xunit3.FrozenAttribute;
using Cabazure.Test.Tests.Fixture;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Attributes;

public class AutoNSubstituteDataAttributeTests
{
    [Theory, AutoNSubstituteData]
    public void Theory_ProvidesStringArgument(string value)
    {
        value.Should().NotBeNullOrEmpty();
    }

    [Theory, AutoNSubstituteData]
    public void Theory_ProvidesIntArgument(int value)
    {
        ((object)value).Should().BeOfType<int>();
    }

    [Theory, AutoNSubstituteData]
    public void Theory_ProvidesInterfaceSubstitute(FixtureFactoryTests.IMyInterface service)
    {
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<FixtureFactoryTests.IMyInterface>();
    }

    [Theory, AutoNSubstituteData]
    public void Theory_WithFrozenParameter_InjectsSameInstanceIntoSut(
        [Frozen] FixtureFactoryTests.IMyInterface service,
        FixtureFactoryTests.MyServiceWithDependency sut)
    {
        sut.Dependency.Should().BeSameAs(service);
    }

    [Theory, AutoNSubstituteData]
    public void Theory_MultipleParameters_AreAllProvided(
        string str,
        int number,
        FixtureFactoryTests.IMyInterface service)
    {
        str.Should().NotBeNullOrEmpty();
        ((object)number).Should().BeOfType<int>();
        service.Should().NotBeNull();
    }

    [Theory, AutoNSubstituteData]
    public void Theory_WithNoAutoProperties_DoesNotPopulateProperties(
        [NoAutoProperties] TypeWithProperties sut)
    {
        sut.Should().NotBeNull();
        sut.Name.Should().BeNull("NoAutoProperties suppresses property population");
        sut.Value.Should().Be(0, "NoAutoProperties suppresses property population");
    }

    [Theory, AutoNSubstituteData]
    public void Theory_WithNoAutoPropertiesAndFrozen_BothApplied(
        [NoAutoProperties, Frozen] TypeWithProperties frozen,
        TypeWithProperties sut)
    {
        sut.Should().BeSameAs(frozen, "[Frozen] freezes the instance");
        sut.Name.Should().BeNull("[NoAutoProperties] was applied before freezing");
    }

    [Theory, AutoNSubstituteData]
    public void Theory_WithoutNoAutoProperties_DoesPopulateProperties(TypeWithProperties sut)
    {
        sut.Name.Should().NotBeNull("AutoFixture populates properties by default");
    }

    public sealed class TypeWithProperties
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}
