using System.Collections;
using FluentAssertions;
using Xunit.Sdk;

namespace Cabazure.Test.Tests.Attributes;

public class DiscoveryEnumerationLabelTests
{
    [Fact]
    public async Task AutoNSubstituteData_ReturnsRow_WithEmptyLabel()
    {
        var disposalTracker = new DisposalTracker();
        var methodInfo = typeof(LabelTestMethodHost)
            .GetMethod(nameof(LabelTestMethodHost.WithString))!;
        var attr = new AutoNSubstituteDataAttribute();

        var rows = await attr.GetData(methodInfo, disposalTracker);

        rows.Single().Label.Should().Be("");
    }

    [Fact]
    public async Task AutoNSubstituteData_CalledTwice_ReturnsSameLabelBothTimes()
    {
        var disposalTracker = new DisposalTracker();
        var methodInfo = typeof(LabelTestMethodHost)
            .GetMethod(nameof(LabelTestMethodHost.WithString))!;
        var attr = new AutoNSubstituteDataAttribute();

        var rows1 = await attr.GetData(methodInfo, disposalTracker);
        var rows2 = await attr.GetData(methodInfo, disposalTracker);

        var label1 = rows1.Single().Label;
        var label2 = rows2.Single().Label;
        label1.Should().Be(label2);
        label1.Should().Be("");
    }

    [Fact]
    public async Task InlineAutoNSubstituteData_ReturnsRow_WithLabelFromInlineValues()
    {
        var disposalTracker = new DisposalTracker();
        var methodInfo = typeof(LabelTestMethodHost)
            .GetMethod(nameof(LabelTestMethodHost.WithStringAndInt))!;
        var attr = new InlineAutoNSubstituteDataAttribute("hello", 42);

        var rows = await attr.GetData(methodInfo, disposalTracker);

        rows.Single().Label.Should().Be("\"hello\", 42");
    }

    [Fact]
    public async Task InlineAutoNSubstituteData_CalledTwice_ReturnsSameLabelBothTimes()
    {
        var disposalTracker = new DisposalTracker();
        var methodInfo = typeof(LabelTestMethodHost)
            .GetMethod(nameof(LabelTestMethodHost.WithStringAndInt))!;
        var attr = new InlineAutoNSubstituteDataAttribute("hello", 42);

        var rows1 = await attr.GetData(methodInfo, disposalTracker);
        var rows2 = await attr.GetData(methodInfo, disposalTracker);

        rows1.Single().Label.Should().Be(rows2.Single().Label);
        rows1.Single().Label.Should().Be("\"hello\", 42");
    }

    [Fact]
    public async Task InlineAutoNSubstituteData_NullInlineValue_FormatsAsNullInLabel()
    {
        var disposalTracker = new DisposalTracker();
        var methodInfo = typeof(LabelTestMethodHost)
            .GetMethod(nameof(LabelTestMethodHost.WithString))!;
        var attr = new InlineAutoNSubstituteDataAttribute((object?)null);

        var rows = await attr.GetData(methodInfo, disposalTracker);

        rows.Single().Label.Should().Be("null");
    }

    [Fact]
    public async Task MemberAutoNSubstituteData_MultipleRows_EachRowLabelIsItsIndex()
    {
        var disposalTracker = new DisposalTracker();
        var methodInfo = typeof(MemberLabelTestHost)
            .GetMethod(nameof(MemberLabelTestHost.WithString))!;
        var attr = new MemberAutoNSubstituteDataAttribute(nameof(MemberLabelTestHost.ThreeStringRows));

        var rows = (await attr.GetData(methodInfo, disposalTracker)).ToList();

        rows[0].Label.Should().Be("0");
        rows[1].Label.Should().Be("1");
        rows[2].Label.Should().Be("2");
    }

    [Fact]
    public async Task MemberAutoNSubstituteData_CalledTwice_ReturnsSameLabelsInOrder()
    {
        var disposalTracker = new DisposalTracker();
        var methodInfo = typeof(MemberLabelTestHost)
            .GetMethod(nameof(MemberLabelTestHost.WithString))!;
        var attr = new MemberAutoNSubstituteDataAttribute(nameof(MemberLabelTestHost.ThreeStringRows));

        var rows1 = (await attr.GetData(methodInfo, disposalTracker)).ToList();
        var rows2 = (await attr.GetData(methodInfo, disposalTracker)).ToList();

        for (var i = 0; i < 3; i++)
            rows1[i].Label.Should().Be(rows2[i].Label);
    }

    [Fact]
    public async Task ClassAutoNSubstituteData_MultipleRows_EachRowLabelIsItsIndex()
    {
        var disposalTracker = new DisposalTracker();
        var methodInfo = typeof(LabelTestMethodHost)
            .GetMethod(nameof(LabelTestMethodHost.WithString))!;
        var attr = new ClassAutoNSubstituteDataAttribute(typeof(ThreeRowClassData));

        var rows = (await attr.GetData(methodInfo, disposalTracker)).ToList();

        rows[0].Label.Should().Be("0");
        rows[1].Label.Should().Be("1");
        rows[2].Label.Should().Be("2");
    }

    [Fact]
    public async Task ClassAutoNSubstituteData_CalledTwice_ReturnsSameLabelsInOrder()
    {
        var disposalTracker = new DisposalTracker();
        var methodInfo = typeof(LabelTestMethodHost)
            .GetMethod(nameof(LabelTestMethodHost.WithString))!;
        var attr = new ClassAutoNSubstituteDataAttribute(typeof(ThreeRowClassData));

        var rows1 = (await attr.GetData(methodInfo, disposalTracker)).ToList();
        var rows2 = (await attr.GetData(methodInfo, disposalTracker)).ToList();

        for (var i = 0; i < 3; i++)
            rows1[i].Label.Should().Be(rows2[i].Label);
    }

    private static class LabelTestMethodHost
    {
        public static void WithString(string value) { }
        public static void WithStringAndInt(string s, int n, IDisposable d) { }
    }

    private static class MemberLabelTestHost
    {
        public static void WithString(string value) { }

        public static IEnumerable<object[]> ThreeStringRows()
        {
            yield return new object[] { "row-a" };
            yield return new object[] { "row-b" };
            yield return new object[] { "row-c" };
        }
    }

    public class ThreeRowClassData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "row-a" };
            yield return new object[] { "row-b" };
            yield return new object[] { "row-c" };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
