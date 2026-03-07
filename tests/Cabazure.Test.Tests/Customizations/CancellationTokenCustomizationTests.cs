using AutoFixture;
using Cabazure.Test.Customizations;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Customizations;

public class CancellationTokenCustomizationTests
{
    [Fact]
    public void CancellationTokenCustomization_CreatesToken_WithIsCancellationRequestedFalse()
    {
        var fixture = FixtureFactory.Create();

        var token = fixture.Create<CancellationToken>();

        token.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void CancellationTokenCustomization_CreatesToken_WithCanBeCanceledFalse()
    {
        var fixture = FixtureFactory.Create();

        var token = fixture.Create<CancellationToken>();

        token.CanBeCanceled.Should().BeFalse();
    }

    [Fact]
    public void CancellationTokenCustomization_CreatesTwoTokens_ThatAreEqual()
    {
        var fixture = FixtureFactory.Create();

        var first = fixture.Create<CancellationToken>();
        var second = fixture.Create<CancellationToken>();

        first.Should().Be(second);
        first.Should().Be(CancellationToken.None);
        second.Should().Be(CancellationToken.None);
    }

    [Fact]
    public void CancellationTokenCustomization_WhenRemoved_AllowsAutoFixtureDefault()
    {
        var removed = FixtureFactory.Customizations.Remove<CancellationTokenCustomization>();
        removed.Should().BeTrue("customization should be registered by default");

        try
        {
            var fixture = FixtureFactory.Create();

            var token = fixture.Create<CancellationToken>();

            token.IsCancellationRequested.Should().BeTrue(
                "AutoFixture default produces new CancellationToken(true)");
        }
        finally
        {
            // Restore to avoid cross-test pollution
            FixtureFactory.Customizations.Add(new CancellationTokenCustomization());
        }
    }

    [Theory, AutoNSubstituteData]
    public void CancellationTokenCustomization_WithAutoNSubstituteData_ProvidesNonCancelledToken(
        CancellationToken ct)
    {
        ct.IsCancellationRequested.Should().BeFalse();
    }
}
