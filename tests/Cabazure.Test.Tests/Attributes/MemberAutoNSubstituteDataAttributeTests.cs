using Cabazure.Test.Attributes;
using Cabazure.Test.Tests.Fixture;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Attributes;

public class MemberAutoNSubstituteDataAttributeTests
{
    public static IEnumerable<object[]> StringRows =>
    [
        ["hello"],
        ["world"],
    ];

    public static IEnumerable<object[]> MultiValueRows =>
    [
        ["hello", 1],
        ["world", 2],
    ];

    [Theory]
    [MemberAutoNSubstituteData(nameof(StringRows))]
    public void MemberProperty_ProvidesRows(
        string value,
        SutFixtureTests.IMyInterface service)
    {
        value.Should().BeOneOf("hello", "world");
        service.Should().NotBeNull();
    }

    [Theory]
    [MemberAutoNSubstituteData(nameof(MultiValueRows))]
    public void MemberProperty_MultipleColumns_ArePassedThrough(
        string message,
        int count,
        SutFixtureTests.IMyInterface service)
    {
        message.Should().BeOneOf("hello", "world");
        count.Should().BeOneOf(1, 2);
        service.Should().NotBeNull();
    }

    [Theory]
    [MemberAutoNSubstituteData(nameof(StringRows))]
    public void FrozenAutoParameter_InjectsSameInstanceIntoSut(
        string _,
        [Frozen] SutFixtureTests.IMyInterface service,
        SutFixtureTests.MyServiceWithDependency sut)
    {
        sut.Dependency.Should().BeSameAs(service);
    }

    [Theory]
    [MemberAutoNSubstituteData("ExternalRows", MemberType = typeof(ExternalMemberData))]
    public void MemberType_ResolvesOnExternalType(
        string value,
        SutFixtureTests.IMyInterface service)
    {
        value.Should().Be("external");
        service.Should().NotBeNull();
    }

    public static IEnumerable<object[]> MethodRows(string prefix) =>
    [
        [$"{prefix}1"],
        [$"{prefix}2"],
    ];

    [Theory]
    [MemberAutoNSubstituteData(nameof(MethodRows), "item")]
    public void MemberMethod_WithParameters_ProvidesRows(
        string value,
        SutFixtureTests.IMyInterface service)
    {
        value.Should().BeOneOf("item1", "item2");
        service.Should().NotBeNull();
    }
}

public static class ExternalMemberData
{
    public static IEnumerable<object[]> ExternalRows =>
    [
        ["external"],
    ];
}
