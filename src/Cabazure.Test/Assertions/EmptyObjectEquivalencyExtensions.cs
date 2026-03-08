using FluentAssertions.Equivalency;

namespace Cabazure.Test;

/// <summary>
/// Extension methods for registering <see cref="EmptyObjectEquivalencyStep"/> with FluentAssertions
/// equivalency assertion options.
/// </summary>
public static class EmptyObjectEquivalencyExtensions
{
    /// <summary>
    /// Configures the equivalency assertion options to allow <c>BeEquivalentTo</c> to succeed when
    /// comparing instances of types that have no public properties or fields.
    /// </summary>
    /// <typeparam name="TSelf">
    /// The concrete options type, either <see cref="EquivalencyAssertionOptions{TExpectation}"/>
    /// (for per-call use) or <see cref="EquivalencyAssertionOptions"/> (for global use).
    /// </typeparam>
    /// <param name="options">The equivalency assertion options to configure.</param>
    /// <returns>
    /// The same <paramref name="options"/> instance to allow method chaining.
    /// </returns>
    /// <example>
    /// <para>
    /// <b>Per-call usage</b> — applies only to this single assertion:
    /// <code>
    /// result.Should().BeEquivalentTo(expected, opts => opts.AllowingEmptyObjects());
    /// </code>
    /// </para>
    /// <para>
    /// <b>Global usage</b> — applies to all equivalency assertions in the test session (e.g. in a
    /// module initializer):
    /// <code>
    /// AssertionOptions.AssertEquivalencyUsing(opts => opts.AllowingEmptyObjects());
    /// </code>
    /// </para>
    /// </example>
    public static TSelf AllowingEmptyObjects<TSelf>(
        this SelfReferenceEquivalencyAssertionOptions<TSelf> options)
        where TSelf : SelfReferenceEquivalencyAssertionOptions<TSelf>
        => options.Using(new EmptyObjectEquivalencyStep());
}
