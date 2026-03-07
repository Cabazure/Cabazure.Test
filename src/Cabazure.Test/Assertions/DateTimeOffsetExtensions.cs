using FluentAssertions;
using FluentAssertions.Primitives;

namespace Cabazure.Test;

/// <summary>
/// Provides configurable precision options for Cabazure.Test assertions.
/// These options can be set in a <c>[ModuleInitializer]</c> method or in a test setup
/// method (e.g., constructor, xUnit fixture, or a global test initializer).
/// </summary>
public static class CabazureAssertionOptions
{
    /// <summary>
    /// Gets or sets the default precision used when comparing <see cref="DateTimeOffset"/>
    /// values via the <see cref="DateTimeOffsetExtensions.BeCloseTo{TAssertions}(DateTimeOffsetAssertions{TAssertions}, DateTimeOffset, string, object[])"/>
    /// and <see cref="DateTimeOffsetExtensions.NotBeCloseTo{TAssertions}(DateTimeOffsetAssertions{TAssertions}, DateTimeOffset, string, object[])"/>
    /// extension methods. Defaults to <c>1 second</c>.
    /// </summary>
    public static TimeSpan DateTimeOffsetPrecision { get; set; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Extension methods for <see cref="DateTimeOffsetAssertions{TAssertions}"/> that provide
/// configurable-precision comparison overloads.
/// </summary>
public static class DateTimeOffsetExtensions
{
    /// <summary>
    /// Asserts that a <see cref="DateTimeOffset"/> is close to another value using the
    /// default precision specified by <see cref="CabazureAssertionOptions.DateTimeOffsetPrecision"/>.
    /// </summary>
    /// <typeparam name="TAssertions">The type of assertions class.</typeparam>
    /// <param name="assertions">The assertions instance.</param>
    /// <param name="nearbyTime">
    /// The expected time to compare the actual value with. The actual value must be within
    /// the default precision of this time.
    /// </param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    public static AndConstraint<TAssertions> BeCloseTo<TAssertions>(
        this DateTimeOffsetAssertions<TAssertions> assertions,
        DateTimeOffset nearbyTime,
        string because = "",
        params object[] becauseArgs)
        where TAssertions : DateTimeOffsetAssertions<TAssertions>
    {
        return assertions.BeCloseTo(nearbyTime, CabazureAssertionOptions.DateTimeOffsetPrecision, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that a <see cref="DateTimeOffset"/> is close to another value using a
    /// precision specified in milliseconds.
    /// </summary>
    /// <typeparam name="TAssertions">The type of assertions class.</typeparam>
    /// <param name="assertions">The assertions instance.</param>
    /// <param name="nearbyTime">
    /// The expected time to compare the actual value with. The actual value must be within
    /// the specified precision of this time.
    /// </param>
    /// <param name="precisionMilliseconds">The precision in milliseconds.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    public static AndConstraint<TAssertions> BeCloseTo<TAssertions>(
        this DateTimeOffsetAssertions<TAssertions> assertions,
        DateTimeOffset nearbyTime,
        int precisionMilliseconds,
        string because = "",
        params object[] becauseArgs)
        where TAssertions : DateTimeOffsetAssertions<TAssertions>
    {
        return assertions.BeCloseTo(nearbyTime, TimeSpan.FromMilliseconds(precisionMilliseconds), because, becauseArgs);
    }

    /// <summary>
    /// Asserts that a <see cref="DateTimeOffset"/> is not close to another value using the
    /// default precision specified by <see cref="CabazureAssertionOptions.DateTimeOffsetPrecision"/>.
    /// </summary>
    /// <typeparam name="TAssertions">The type of assertions class.</typeparam>
    /// <param name="assertions">The assertions instance.</param>
    /// <param name="distantTime">
    /// The time to compare the actual value with. The actual value must not be within
    /// the default precision of this time.
    /// </param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    public static AndConstraint<TAssertions> NotBeCloseTo<TAssertions>(
        this DateTimeOffsetAssertions<TAssertions> assertions,
        DateTimeOffset distantTime,
        string because = "",
        params object[] becauseArgs)
        where TAssertions : DateTimeOffsetAssertions<TAssertions>
    {
        return assertions.NotBeCloseTo(distantTime, CabazureAssertionOptions.DateTimeOffsetPrecision, because, becauseArgs);
    }

    /// <summary>
    /// Asserts that a <see cref="DateTimeOffset"/> is not close to another value using a
    /// precision specified in milliseconds.
    /// </summary>
    /// <typeparam name="TAssertions">The type of assertions class.</typeparam>
    /// <param name="assertions">The assertions instance.</param>
    /// <param name="distantTime">
    /// The time to compare the actual value with. The actual value must not be within
    /// the specified precision of this time.
    /// </param>
    /// <param name="precisionMilliseconds">The precision in milliseconds.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])"/>
    /// explaining why the assertion is needed. If the phrase does not start with the word
    /// <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <returns>An <see cref="AndConstraint{T}"/> to support chaining.</returns>
    public static AndConstraint<TAssertions> NotBeCloseTo<TAssertions>(
        this DateTimeOffsetAssertions<TAssertions> assertions,
        DateTimeOffset distantTime,
        int precisionMilliseconds,
        string because = "",
        params object[] becauseArgs)
        where TAssertions : DateTimeOffsetAssertions<TAssertions>
    {
        return assertions.NotBeCloseTo(distantTime, TimeSpan.FromMilliseconds(precisionMilliseconds), because, becauseArgs);
    }
}
