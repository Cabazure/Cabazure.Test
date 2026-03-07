using System.Collections;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Cabazure.Test.Customizations;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Customizations;

public class FixtureCustomizationCollectionTests
{
    // Nested types — only ever requested by this test class, so registering a
    // customization for them in the global registry cannot affect other tests.
    public record CustomizedDomainValue(string Tag);

    public sealed class GlobalCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
            => fixture.Inject(new CustomizedDomainValue("from-global"));
    }

    // Static constructor runs exactly once, before any test in this class.
    static FixtureCustomizationCollectionTests()
        => FixtureFactory.Customizations.Add(new GlobalCustomization());

    [Fact]
    public void Add_Null_Throws()
    {
        var act = () => FixtureFactory.Customizations.Add((ICustomization)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory, AutoNSubstituteData]
    public void RegisteredCustomization_IsApplied_ByAutoNSubstituteData(
        CustomizedDomainValue value)
    {
        value.Tag.Should().Be("from-global");
    }

    [Theory]
    [InlineAutoNSubstituteData]
    public void RegisteredCustomization_IsApplied_ByInlineAutoNSubstituteData(
        CustomizedDomainValue value)
    {
        value.Tag.Should().Be("from-global");
    }

    public static IEnumerable<object[]> EmptyRows => [[]];

    [Theory]
    [MemberAutoNSubstituteData(nameof(EmptyRows))]
    public void RegisteredCustomization_IsApplied_ByMemberAutoNSubstituteData(
        CustomizedDomainValue value)
    {
        value.Tag.Should().Be("from-global");
    }

    public class EmptyDataClass : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator() { yield return []; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassAutoNSubstituteData(typeof(EmptyDataClass))]
    public void RegisteredCustomization_IsApplied_ByClassAutoNSubstituteData(
        CustomizedDomainValue value)
    {
        value.Tag.Should().Be("from-global");
    }

    // Verifies the end-to-end chain: ModuleInitializer → FixtureFactory.Customizations →
    // AutoNSubstituteData. ProjectWideValue is registered by
    // TestAssemblyInitializer.Initialize() which runs before any tests.
    [Theory, AutoNSubstituteData]
    public void ProjectWideCustomization_IsApplied_WhenAutoNSubstituteDataUsed(
        ProjectWideValue value)
    {
        value.Text.Should().Be("project-wide");
    }

    [Fact]
    public void AfterModuleInitializer_ContainsProjectWideCustomization()
    {
        FixtureFactory.Customizations
            .Should().Contain(c => c is ProjectWideTestCustomization);
    }

    [Fact]
    public void Add_MultipleCustomizations_CountGrowsByExactAmount()
    {
        var countBefore = FixtureFactory.Customizations.Count;

        FixtureFactory.Customizations.Add(new CountTestCustomization());
        FixtureFactory.Customizations.Add(new CountTestCustomization());

        FixtureFactory.Customizations.Count.Should().Be(countBefore + 2);
    }

    [Fact]
    public void Remove_ExistingInstance_RemovesIt_ReturnsTrue()
    {
        var customization = new CountTestCustomization();
        FixtureFactory.Customizations.Add(customization);
        var countBefore = FixtureFactory.Customizations.Count;

        var result = FixtureFactory.Customizations.Remove(customization);

        result.Should().BeTrue();
        FixtureFactory.Customizations.Count.Should().Be(countBefore - 1);
        FixtureFactory.Customizations.Should().NotContain(customization);
    }

    [Fact]
    public void Remove_NonExistentInstance_ReturnsFalse()
    {
        var customization = new CountTestCustomization(); // never added

        var result = FixtureFactory.Customizations.Remove(customization);

        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveT_ExistingType_RemovesFirstMatch_ReturnsTrue()
    {
        FixtureFactory.Customizations.Add(new RemovableCustomization());
        var countBefore = FixtureFactory.Customizations.Count;

        var result = FixtureFactory.Customizations.Remove<RemovableCustomization>();

        result.Should().BeTrue();
        FixtureFactory.Customizations.Count.Should().Be(countBefore - 1);
        FixtureFactory.Customizations.Should().NotContain(c => c is RemovableCustomization);
    }

    [Fact]
    public void RemoveT_NonExistentType_ReturnsFalse()
    {
        // Ensure type is not present before the test
        while (FixtureFactory.Customizations.Remove<UniqueAbsentCustomization>()) { }

        var result = FixtureFactory.Customizations.Remove<UniqueAbsentCustomization>();

        result.Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var collection = new FixtureCustomizationCollection();
        collection.Add(new CountTestCustomization());

        collection.Clear();

        collection.Count.Should().Be(0);
    }

    [Fact]
    public void Enumerable_ReturnsSnapshot_NotLive()
    {
        FixtureFactory.Customizations.Add(new CountTestCustomization());
        var countAtEnumStart = 0;
        var countDuringIteration = 0;

        foreach (var item in FixtureFactory.Customizations)
        {
            if (countAtEnumStart == 0)
            {
                countAtEnumStart = FixtureFactory.Customizations.Count;
                FixtureFactory.Customizations.Add(new CountTestCustomization());
                countDuringIteration = FixtureFactory.Customizations.Count;
            }
        }

        // The count grew during iteration, proving enumeration was over a snapshot
        countDuringIteration.Should().BeGreaterThan(countAtEnumStart);
    }

    [Fact]
    public void Remove_Null_Throws()
    {
        var act = () => FixtureFactory.Customizations.Remove(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class CountTestCustomization : ICustomization
    {
        public void Customize(IFixture fixture) { }
    }

    private sealed class RemovableCustomization : ICustomization
    {
        public void Customize(IFixture fixture) { }
    }

    private sealed class UniqueAbsentCustomization : ICustomization
    {
        public void Customize(IFixture fixture) { }
    }
}
