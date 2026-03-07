using AutoFixture;
using Cabazure.Test.Attributes;
using Cabazure.Test.Tests.Fixture;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Attributes;

public class AutoNSubstituteDataHelperFixtureInjectionTests
{
    // Test 1: IFixture parameter is not null
    [Theory, AutoNSubstituteData]
    public void Theory_IFixtureParameter_IsNotNull(IFixture fixture)
    {
        fixture.Should().NotBeNull();
    }

    // Test 2: IFixture parameter is a concrete Fixture instance
    [Theory, AutoNSubstituteData]
    public void Theory_IFixtureParameter_IsFixtureInstance(IFixture fixture)
    {
        fixture.Should().BeAssignableTo<AutoFixture.Fixture>();
    }

    // Test 3: IFixture is the same instance used to resolve other parameters
    [Theory, AutoNSubstituteData]
    public void Theory_IFixtureParameter_IsSameInstanceResolvingOtherParams(
        [Frozen] FixtureFactoryTests.IMyInterface service,
        IFixture fixture)
    {
        fixture.Create<FixtureFactoryTests.IMyInterface>()
            .Should().BeSameAs(service);
    }

    // Test 4: Concrete Fixture type parameter also works
    [Theory, AutoNSubstituteData]
    public void Theory_ConcreteFixtureParameter_IsInjected(AutoFixture.Fixture fixture)
    {
        fixture.Should().NotBeNull();
    }

    // Test 5: InlineAutoNSubstituteData fills earlier params; IFixture is auto-filled as the fixture
    [Theory]
    [InlineAutoNSubstituteData("hello")]
    public void InlineData_WithIFixtureParameter_InjectsFixture(string value, IFixture fixture)
    {
        value.Should().Be("hello");
        fixture.Should().NotBeNull();
        fixture.Should().BeAssignableTo<AutoFixture.Fixture>();
    }

    // Test 6: [Frozen] on IFixture parameter does not throw; fixture is injected normally
    [Theory, AutoNSubstituteData]
    public void Theory_FrozenIFixtureParameter_IsInjectedNormally([Frozen] IFixture fixture)
    {
        fixture.Should().NotBeNull();
        fixture.Should().BeAssignableTo<AutoFixture.Fixture>();
    }
}
