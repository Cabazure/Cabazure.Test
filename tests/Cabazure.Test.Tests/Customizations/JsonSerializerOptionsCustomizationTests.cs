using System.Text.Json;
using AutoFixture;
using Cabazure.Test.Customizations;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Customizations;

public class JsonSerializerOptionsCustomizationTests
{
    [Fact]
    public void Create_JsonSerializerOptions_DoesNotThrow()
    {
        var fixture = FixtureFactory.Create();

        var act = () => fixture.Create<JsonSerializerOptions>();

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_JsonSerializerOptions_HasValidIndentCharacter()
    {
        var fixture = FixtureFactory.Create();

        var options = fixture.Create<JsonSerializerOptions>();

        options.IndentCharacter.Should().BeOneOf(' ', '\t');
    }

    [Fact]
    public void Create_JsonSerializerOptions_IsNotReadOnly()
    {
        var fixture = FixtureFactory.Create();

        var options = fixture.Create<JsonSerializerOptions>();

        options.IsReadOnly.Should().BeFalse();
    }

    [Fact]
    public void Create_JsonSerializerOptions_CreatesTwoSeparateInstances()
    {
        var fixture = FixtureFactory.Create();

        var first = fixture.Create<JsonSerializerOptions>();
        var second = fixture.Create<JsonSerializerOptions>();

        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public void WhenRemoved_AllowsAutoFixtureDefault_WhichThrows()
    {
        var removed = FixtureFactory.Customizations.Remove<JsonSerializerOptionsCustomization>();
        removed.Should().BeTrue("customization should be registered by default");

        try
        {
            var fixture = FixtureFactory.Create();

            var act = () => fixture.Create<JsonSerializerOptions>();

            act.Should().Throw<Exception>(
                "AutoFixture default sets IndentCharacter to a random char, which fails validation");
        }
        finally
        {
            FixtureFactory.Customizations.Add(new JsonSerializerOptionsCustomization());
        }
    }

    [Theory, AutoNSubstituteData]
    public void AutoNSubstituteData_WithJsonSerializerOptionsParameter_DoesNotThrow(
        JsonSerializerOptions options)
    {
        options.Should().NotBeNull();
        options.IndentCharacter.Should().BeOneOf(' ', '\t');
    }
}
