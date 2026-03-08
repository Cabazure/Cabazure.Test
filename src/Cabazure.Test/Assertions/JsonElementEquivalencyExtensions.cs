using FluentAssertions.Equivalency;

namespace Cabazure.Test;

/// <summary>
/// Extension methods for registering <see cref="JsonElementEquivalencyStep"/> with FluentAssertions
/// equivalency assertion options.
/// </summary>
public static class JsonElementEquivalencyExtensions
{
    /// <summary>
    /// Configures the equivalency assertion options to use <see cref="JsonElementEquivalencyStep"/>
    /// for semantic comparison of <see cref="System.Text.Json.JsonElement"/> properties.
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
    /// result.Should().BeEquivalentTo(expected, opts => opts.UsingJsonElementComparison());
    /// </code>
    /// </para>
    /// <para>
    /// <b>Global usage</b> — applies to all equivalency assertions in the test session (e.g. in
    /// an xUnit <c>AssemblyFixture</c> or test class constructor):
    /// <code>
    /// AssertionOptions.AssertEquivalencyUsing(opts => opts.UsingJsonElementComparison());
    /// </code>
    /// </para>
    /// </example>
    public static TSelf UsingJsonElementComparison<TSelf>(
        this SelfReferenceEquivalencyAssertionOptions<TSelf> options)
        where TSelf : SelfReferenceEquivalencyAssertionOptions<TSelf>
        => options.Using(new JsonElementEquivalencyStep());
}
