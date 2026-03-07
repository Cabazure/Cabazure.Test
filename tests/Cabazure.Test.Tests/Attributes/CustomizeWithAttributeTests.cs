using System.Collections;
using System.Reflection;
using AutoFixture;
using Cabazure.Test.Attributes;
using FluentAssertions;
using Xunit.Sdk;

namespace Cabazure.Test.Tests.Attributes;

public class CustomizeWithAttributeTests
{
    // ---- Helper types for method-level tests ----

    public record MethodLevelValue(string Tag);

    public sealed class MethodLevelCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
            => fixture.Inject(new MethodLevelValue("method-level"));
    }

    // ---- Helper types for multiple-customization test ----

    public record FirstValue(string Tag);
    public record SecondValue(string Tag);

    public sealed class FirstCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
            => fixture.Inject(new FirstValue("first"));
    }

    public sealed class SecondCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
            => fixture.Inject(new SecondValue("second"));
    }

    // ---- Helper types for class-level test ----

    public record ClassLevelValue(string Tag);

    public sealed class ClassLevelCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
            => fixture.Inject(new ClassLevelValue("class-level"));
    }

    // ---- Tests ----

    [Theory, AutoNSubstituteData]
    [CustomizeWith(typeof(MethodLevelCustomization))]
    public void MethodLevel_CustomizeWith_IsApplied(MethodLevelValue value)
    {
        value.Tag.Should().Be("method-level");
    }

    [Fact]
    public void Constructor_WithNullType_ThrowsArgumentNullException()
    {
        Action act = () => new CustomizeWithAttribute(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Instantiate_WithTypeWithoutPublicParameterlessCtor_ThrowsInvalidOperationException()
    {
        var attr = new CustomizeWithAttribute(typeof(NoParameterlessCtorCustomization));

        Action act = () => attr.Instantiate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*public parameterless constructor*");
    }

    [Theory, AutoNSubstituteData]
    [CustomizeWith(typeof(FirstCustomization))]
    [CustomizeWith(typeof(SecondCustomization))]
    public void MultipleCustomizations_AreAllApplied(FirstValue first, SecondValue second)
    {
        first.Tag.Should().Be("first");
        second.Tag.Should().Be("second");
    }

    // This private static method is decorated with an invalid customization type
    // (string does not implement ICustomization). It serves as a reflective target
    // for the test below, which verifies that GetData throws InvalidOperationException.
    [CustomizeWith(typeof(string))]
    private static void MethodWithInvalidCustomizationType() { }

    [Fact]
    public void CustomizeWith_WithNonCustomizationType_ThrowsInvalidOperationException()
    {
        var attr = new AutoNSubstituteDataAttribute();
        var method = typeof(CustomizeWithAttributeTests)
            .GetMethod(
                nameof(MethodWithInvalidCustomizationType),
                BindingFlags.NonPublic | BindingFlags.Static)!;

        // CreateFixture throws synchronously when it encounters a type that does
        // not implement ICustomization, so the exception propagates from GetData.
        var act = () => { attr.GetData(method, new DisposalTracker()); };
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not implement*");
    }

    [Theory]
    [InlineAutoNSubstituteData("known")]
    [CustomizeWith(typeof(MethodLevelCustomization))]
    public void CustomizeWith_IsApplied_ByInlineAutoNSubstituteData(
        string inlineStr,
        MethodLevelValue value)
    {
        inlineStr.Should().Be("known");
        value.Tag.Should().Be("method-level");
    }

    public static IEnumerable<object[]> MethodLevelRows => [["member-row"]];

    [Theory]
    [MemberAutoNSubstituteData(nameof(MethodLevelRows))]
    [CustomizeWith(typeof(MethodLevelCustomization))]
    public void CustomizeWith_IsApplied_ByMemberAutoNSubstituteData(
        string row,
        MethodLevelValue value)
    {
        row.Should().Be("member-row");
        value.Tag.Should().Be("method-level");
    }

    public class MethodLevelDataClass : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator() { yield return ["class-row"]; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassAutoNSubstituteData(typeof(MethodLevelDataClass))]
    [CustomizeWith(typeof(MethodLevelCustomization))]
    public void CustomizeWith_IsApplied_ByClassAutoNSubstituteData(
        string row,
        MethodLevelValue value)
    {
        row.Should().Be("class-row");
        value.Tag.Should().Be("method-level");
    }

    // ---- Nested class for class-level [CustomizeWith] test ----

    [CustomizeWith(typeof(ClassLevelCustomization))]
    public class ClassLevelTests
    {
        [Theory, AutoNSubstituteData]
        public void ClassLevel_CustomizeWith_IsApplied(ClassLevelValue value)
        {
            value.Tag.Should().Be("class-level");
        }
    }
}

internal sealed class NoParameterlessCtorCustomization : ICustomization
{
    public NoParameterlessCtorCustomization(string value) { }
    public void Customize(IFixture fixture) { }
}
