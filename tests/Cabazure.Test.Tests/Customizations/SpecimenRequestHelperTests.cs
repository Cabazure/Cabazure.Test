using Cabazure.Test.Customizations;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Customizations;

public class SpecimenRequestHelperTests
{
    private class TestSubject
    {
        public string? SomeProperty { get; set; }
#pragma warning disable CS0649 // Field is never assigned
        public int SomeField;
#pragma warning restore CS0649
        public TestSubject(bool someParameter) { }
    }

    [Fact]
    public void GetRequestType_ReturnsParameterType_WhenRequestIsParameterInfo()
    {
        var parameterInfo = typeof(TestSubject)
            .GetConstructors()[0]
            .GetParameters()[0];

        var result = SpecimenRequestHelper.GetRequestType(parameterInfo);

        result.Should().Be(typeof(bool));
    }

    [Fact]
    public void GetRequestType_ReturnsPropertyType_WhenRequestIsPropertyInfo()
    {
        var propertyInfo = typeof(TestSubject)
            .GetProperty(nameof(TestSubject.SomeProperty))!;

        var result = SpecimenRequestHelper.GetRequestType(propertyInfo);

        result.Should().Be(typeof(string));
    }

    [Fact]
    public void GetRequestType_ReturnsFieldType_WhenRequestIsFieldInfo()
    {
        var fieldInfo = typeof(TestSubject)
            .GetField(nameof(TestSubject.SomeField))!;

        var result = SpecimenRequestHelper.GetRequestType(fieldInfo);

        result.Should().Be(typeof(int));
    }

    [Fact]
    public void GetRequestType_ReturnsType_WhenRequestIsType()
    {
        var result = SpecimenRequestHelper.GetRequestType(typeof(string));

        result.Should().Be(typeof(string));
    }

    [Fact]
    public void GetRequestType_ReturnsNull_WhenRequestIsUnknown()
    {
        var result = SpecimenRequestHelper.GetRequestType("some string");

        result.Should().BeNull();
    }
}
