using System.Collections;
using AutoFixture;
using Cabazure.Test.Attributes;
using Cabazure.Test.Customizations;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Customizations;

public class SutFixtureCustomizationsTests
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
    static SutFixtureCustomizationsTests()
        => SutFixtureCustomizations.Add(new GlobalCustomization());

    [Fact]
    public void Add_Null_Throws()
    {
        var act = () => SutFixtureCustomizations.Add(null!);
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

    // Verifies the end-to-end chain: ModuleInitializer → SutFixtureCustomizations →
    // SutFixture → AutoNSubstituteData. ProjectWideValue is registered by
    // TestAssemblyInitializer.Initialize() which runs before any tests.
    [Theory, AutoNSubstituteData]
    public void ProjectWideCustomization_IsApplied_WhenAutoNSubstituteDataUsed(
        ProjectWideValue value)
    {
        value.Text.Should().Be("project-wide");
    }

    [Fact]
    public void All_AfterModuleInitializer_ContainsProjectWideCustomization()
    {
        SutFixtureCustomizations.All
            .Should().Contain(c => c is ProjectWideTestCustomization);
    }

    [Fact]
    public void Add_MultipleCustomizations_AllCountGrowsByExactAmount()
    {
        var countBefore = SutFixtureCustomizations.All.Count;

        SutFixtureCustomizations.Add(new CountTestCustomization());
        SutFixtureCustomizations.Add(new CountTestCustomization());

        SutFixtureCustomizations.All.Count.Should().Be(countBefore + 2);
    }

    private sealed class CountTestCustomization : ICustomization
    {
        public void Customize(IFixture fixture) { }
    }
}
