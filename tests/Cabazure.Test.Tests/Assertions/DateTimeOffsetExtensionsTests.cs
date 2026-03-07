using Cabazure.Test;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Assertions;

public class DateTimeOffsetExtensionsTests
{
    [Fact]
    public void BeCloseTo_WithinDefaultPrecision_Passes()
    {
        var time1 = DateTimeOffset.UtcNow;
        var time2 = time1.AddMilliseconds(500);

        var act = () => time1.Should().BeCloseTo(time2);

        act.Should().NotThrow();
    }

    [Fact]
    public void BeCloseTo_BeyondDefaultPrecision_Throws()
    {
        var time1 = DateTimeOffset.UtcNow;
        var time2 = time1.AddSeconds(2);

        var act = () => time1.Should().BeCloseTo(time2);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void BeCloseTo_WithIntMilliseconds_WithinPrecision_Passes()
    {
        var time1 = DateTimeOffset.UtcNow;
        var time2 = time1.AddMilliseconds(50);

        var act = () => time1.Should().BeCloseTo(time2, 100);

        act.Should().NotThrow();
    }

    [Fact]
    public void BeCloseTo_WithIntMilliseconds_BeyondPrecision_Throws()
    {
        var time1 = DateTimeOffset.UtcNow;
        var time2 = time1.AddMilliseconds(200);

        var act = () => time1.Should().BeCloseTo(time2, 100);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void NotBeCloseTo_BeyondDefaultPrecision_Passes()
    {
        var time1 = DateTimeOffset.UtcNow;
        var time2 = time1.AddSeconds(2);

        var act = () => time1.Should().NotBeCloseTo(time2);

        act.Should().NotThrow();
    }

    [Fact]
    public void NotBeCloseTo_WithinDefaultPrecision_Throws()
    {
        var time1 = DateTimeOffset.UtcNow;
        var time2 = time1.AddMilliseconds(500);

        var act = () => time1.Should().NotBeCloseTo(time2);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void NotBeCloseTo_WithIntMilliseconds_BeyondPrecision_Passes()
    {
        var time1 = DateTimeOffset.UtcNow;
        var time2 = time1.AddMilliseconds(200);

        var act = () => time1.Should().NotBeCloseTo(time2, 100);

        act.Should().NotThrow();
    }

    [Fact]
    public void NotBeCloseTo_WithIntMilliseconds_WithinPrecision_Throws()
    {
        var time1 = DateTimeOffset.UtcNow;
        var time2 = time1.AddMilliseconds(50);

        var act = () => time1.Should().NotBeCloseTo(time2, 100);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void BeCloseTo_WithCustomDefaultPrecision_UsesThatPrecision()
    {
        var originalPrecision = CabazureAssertionOptions.DateTimeOffsetPrecision;
        try
        {
            CabazureAssertionOptions.DateTimeOffsetPrecision = TimeSpan.FromMilliseconds(50);

            var time1 = DateTimeOffset.UtcNow;
            var time2Pass = time1.AddMilliseconds(30);
            var time2Fail = time1.AddMilliseconds(100);

            var actPass = () => time1.Should().BeCloseTo(time2Pass);
            var actFail = () => time1.Should().BeCloseTo(time2Fail);

            actPass.Should().NotThrow();
            actFail.Should().Throw<Exception>();
        }
        finally
        {
            CabazureAssertionOptions.DateTimeOffsetPrecision = originalPrecision;
        }
    }
}
