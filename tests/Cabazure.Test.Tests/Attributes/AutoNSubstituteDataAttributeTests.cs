using Cabazure.Test.Attributes;
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
    public void Theory_ProvidesInterfaceSubstitute(SutFixtureTests.IMyInterface service)
    {
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<SutFixtureTests.IMyInterface>();
    }

    [Theory, AutoNSubstituteData]
    public void Theory_WithFrozenParameter_InjectsSameInstanceIntoSut(
        [Frozen] SutFixtureTests.IMyInterface service,
        SutFixtureTests.MyServiceWithDependency sut)
    {
        sut.Dependency.Should().BeSameAs(service);
    }

    [Theory, AutoNSubstituteData]
    public void Theory_MultipleParameters_AreAllProvided(
        string str,
        int number,
        SutFixtureTests.IMyInterface service)
    {
        str.Should().NotBeNullOrEmpty();
        ((object)number).Should().BeOfType<int>();
        service.Should().NotBeNull();
    }
}
