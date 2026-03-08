using NSubstitute.Core;
using NSubstitute.Core.Arguments;

namespace Cabazure.Test;

/// <summary>
/// Provides FluentAssertions-based argument matchers for use with NSubstitute's
/// <see cref="NSubstitute.SubstituteExtensions.Received{T}(T, int)"/> assertions.
/// </summary>
public static class FluentArg
{
    /// <summary>
    /// Creates an argument matcher that uses FluentAssertions to verify the argument.
    /// The assertion failure message is included in NSubstitute's <c>ReceivedCallsException</c>
    /// when the argument does not match.
    /// </summary>
    /// <typeparam name="T">The type of the argument to match.</typeparam>
    /// <param name="assertion">
    /// An action that accepts the argument and throws when the argument does not satisfy
    /// the FluentAssertions assertions (e.g. <c>r.Name.Should().Be("Alice")</c>).
    /// </param>
    /// <returns>A reference placeholder consumed by NSubstitute's argument matching pipeline.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assertion"/> is <c>null</c>.</exception>
    public static ref T? Match<T>(Action<T> assertion)
    {
        if (assertion is null) throw new ArgumentNullException(nameof(assertion));
        return ref ArgumentMatcher.Enqueue<T>(new FluentAssertionArgumentMatcher<T>(assertion));
    }
}

/// <summary>
/// NSubstitute argument matcher that delegates match evaluation to a FluentAssertions assertion action.
/// Implements <see cref="IDescribeNonMatches"/> so that assertion failure messages surface in
/// NSubstitute's <c>ReceivedCallsException</c> rather than being silently swallowed.
/// </summary>
/// <typeparam name="T">The type of the argument being matched.</typeparam>
internal sealed class FluentAssertionArgumentMatcher<T> : IArgumentMatcher<T>, IDescribeNonMatches
{
    private readonly Action<T> assertion;

    public FluentAssertionArgumentMatcher(Action<T> assertion)
    {
        this.assertion = assertion;
    }

    /// <inheritdoc />
    public bool IsSatisfiedBy(T? argument)
    {
        try
        {
            assertion(argument!);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public string DescribeFor(object? argument)
    {
        if (argument is not T typed)
        {
            return $"Expected argument of type {typeof(T).FullName} but received {argument?.GetType().FullName ?? "null"}.";
        }

        try
        {
            assertion(typed);
            return string.Empty;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
