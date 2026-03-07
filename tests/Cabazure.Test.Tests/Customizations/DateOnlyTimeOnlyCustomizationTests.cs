using AutoFixture;
using Cabazure.Test.Customizations;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Customizations;

public class DateOnlyTimeOnlyCustomizationTests
{
    private sealed class HasDateTimeOnlyProperties
    {
        public DateOnly Date { get; set; }
        public TimeOnly Time { get; set; }
    }

    [Fact]
    public void Customize_WithNullFixture_Throws()
    {
        var sut = new DateOnlyTimeOnlyCustomization();

        var act = () => sut.Customize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_DateOnly_ReturnsValidValue()
    {
        var fixture = FixtureFactory.Create(new DateOnlyTimeOnlyCustomization());

        var result = fixture.Create<DateOnly>();

        result.Should().NotBe(DateOnly.MinValue);
    }

    [Fact]
    public void Create_DateOnly_ReturnsNonDefaultValue()
    {
        var fixture = FixtureFactory.Create(new DateOnlyTimeOnlyCustomization());

        var result = fixture.Create<DateOnly>();

        result.Should().NotBe(default(DateOnly));
        result.Year.Should().BeGreaterThan(1);
    }

    [Fact]
    public void Create_TimeOnly_ReturnsNonZeroTicks()
    {
        var fixture = FixtureFactory.Create(new DateOnlyTimeOnlyCustomization());

        var results = new[]
        {
            fixture.Create<TimeOnly>(),
            fixture.Create<TimeOnly>(),
            fixture.Create<TimeOnly>()
        };

        results.Should().Contain(t => t != TimeOnly.MinValue);
    }

    [Fact]
    public void Create_DateOnly_ViaFixtureFactory_Succeeds()
    {
        var fixture = FixtureFactory.Create();

        var result = fixture.Create<DateOnly>();

        result.Should().NotBe(DateOnly.MinValue);
    }

    [Fact]
    public void Create_TimeOnly_ViaFixtureFactory_Succeeds()
    {
        var fixture = FixtureFactory.Create();

        var result = fixture.Create<TimeOnly>();

        result.Should().NotBe(default(TimeOnly));
    }

    [Fact]
    public void Create_ObjectWithDateOnlyAndTimeOnlyProperties_Succeeds()
    {
        var fixture = FixtureFactory.Create();

        var result = fixture.Create<HasDateTimeOnlyProperties>();

        result.Date.Should().NotBe(DateOnly.MinValue);
        result.Time.Should().NotBe(TimeOnly.MinValue);
    }
}
