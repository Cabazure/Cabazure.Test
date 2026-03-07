using Cabazure.Test.Attributes;
using Cabazure.Test.Tests.Fixture;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Attributes;

public class InlineAutoNSubstituteDataAttributeTests
{
    [Theory]
    [InlineAutoNSubstituteData("hello")]
    public void InlineValue_IsPassedThrough(
        string value,
        SutFixtureTests.IMyInterface service)
    {
        value.Should().Be("hello");
        service.Should().NotBeNull();
    }

    [Theory]
    [InlineAutoNSubstituteData("hello", 42)]
    public void MultipleInlineValues_ArePassedThrough(
        string message,
        int count,
        SutFixtureTests.IMyInterface service)
    {
        message.Should().Be("hello");
        count.Should().Be(42);
        service.Should().NotBeNull();
    }

    [Theory]
    [InlineAutoNSubstituteData("first")]
    [InlineAutoNSubstituteData("second")]
    public void MultipleDeclarations_YieldMultipleRows(string value)
    {
        value.Should().BeOneOf("first", "second");
    }

    [Theory]
    [InlineAutoNSubstituteData]
    public void NoInlineValues_AutoGeneratesAllParameters(
        string value,
        SutFixtureTests.IMyInterface service)
    {
        value.Should().NotBeNullOrEmpty();
        service.Should().NotBeNull();
    }

    [Theory]
    [InlineAutoNSubstituteData]
    public void FrozenParameter_InjectsSameInstanceIntoSut(
        [Frozen] SutFixtureTests.IMyInterface service,
        SutFixtureTests.MyServiceWithDependency sut)
    {
        sut.Dependency.Should().BeSameAs(service);
    }
}
