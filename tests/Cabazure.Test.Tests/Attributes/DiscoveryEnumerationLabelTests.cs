using System.Collections;
using FluentAssertions;

namespace Cabazure.Test.Tests.Attributes;

public class DiscoveryEnumerationLabelTests
{
    [Fact]
    public void AutoNSubstituteData_SupportsDiscoveryEnumeration_ReturnsFalse()
    {
        var attr = new AutoNSubstituteDataAttribute();

        attr.SupportsDiscoveryEnumeration().Should().BeFalse();
    }

    [Fact]
    public void InlineAutoNSubstituteData_SupportsDiscoveryEnumeration_ReturnsFalse()
    {
        var attr = new InlineAutoNSubstituteDataAttribute("hello", 42);

        attr.SupportsDiscoveryEnumeration().Should().BeFalse();
    }

    [Fact]
    public void MemberAutoNSubstituteData_SupportsDiscoveryEnumeration_ReturnsFalse()
    {
        var attr = new MemberAutoNSubstituteDataAttribute(nameof(SomeStaticMember));

        attr.SupportsDiscoveryEnumeration().Should().BeFalse();
    }

    [Fact]
    public void ClassAutoNSubstituteData_SupportsDiscoveryEnumeration_ReturnsFalse()
    {
        var attr = new ClassAutoNSubstituteDataAttribute(typeof(SomeDataClass));

        attr.SupportsDiscoveryEnumeration().Should().BeFalse();
    }

    private static IEnumerable<object[]> SomeStaticMember()
    {
        yield return new object[] { "row-a" };
    }

    private class SomeDataClass : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "row-a" };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
