using System.Text.Json;
using FluentAssertions.Equivalency;

namespace Cabazure.Test;

/// <summary>
/// A FluentAssertions <see cref="IEquivalencyStep"/> that handles semantic comparison
/// of <see cref="System.Text.Json.JsonElement"/> values within object graph equivalency assertions.
/// </summary>
/// <remarks>
/// <para>
/// When FluentAssertions performs a structural equivalency check (e.g.
/// <c>result.Should().BeEquivalentTo(expected)</c>), it falls back to reference equality for
/// <see cref="System.Text.Json.JsonElement"/> properties, which may produce false negatives even
/// for semantically identical JSON. This step intercepts those comparisons and normalizes both
/// values via <see cref="JsonElementHelper.ToCompactString"/> (a reflection-free
/// <see cref="System.Text.Json.Utf8JsonWriter"/>-based serializer) before delegating to
/// FluentAssertions' string comparison — providing accurate structural diffs on failure.
/// </para>
/// <para>
/// Register per-call:
/// <code>
/// result.Should().BeEquivalentTo(expected, opts => opts.Using(new JsonElementEquivalencyStep()));
/// </code>
/// Or use the <see cref="JsonElementEquivalencyExtensions.UsingJsonElementComparison{TSelf}"/> extension:
/// <code>
/// result.Should().BeEquivalentTo(expected, opts => opts.UsingJsonElementComparison());
/// </code>
/// </para>
/// <para>
/// Register globally (affects all equivalency assertions in the test session):
/// <code>
/// AssertionOptions.AssertEquivalencyUsing(opts => opts.UsingJsonElementComparison());
/// </code>
/// </para>
/// </remarks>
public sealed class JsonElementEquivalencyStep : IEquivalencyStep
{
    /// <summary>
    /// Handles the equivalency comparison for <see cref="System.Text.Json.JsonElement"/> pairs.
    /// If either comparand is not a <see cref="System.Text.Json.JsonElement"/>, the step is skipped
    /// and the next registered step continues the pipeline.
    /// </summary>
    /// <param name="comparands">
    /// The subject and expectation values to compare.
    /// </param>
    /// <param name="context">
    /// Provides context about the current node in the object graph and the reason for the assertion.
    /// </param>
    /// <param name="nestedValidator">
    /// The validator that can be used to recursively assert nested members — used by this step
    /// to delegate string comparison to FluentAssertions for accurate diff messages.
    /// </param>
    /// <returns>
    /// <see cref="EquivalencyResult.AssertionCompleted"/> if both comparands are
    /// <see cref="System.Text.Json.JsonElement"/> values (assertion performed, pipeline stops);
    /// <see cref="EquivalencyResult.ContinueWithNext"/> otherwise.
    /// </returns>
    public EquivalencyResult Handle(
        Comparands comparands,
        IEquivalencyValidationContext context,
        IEquivalencyValidator nestedValidator)
    {
        if (comparands.Subject is not JsonElement subject
            || comparands.Expectation is not JsonElement expectation)
        {
            return EquivalencyResult.ContinueWithNext;
        }

        var newComparands = new Comparands(
            JsonElementHelper.ToCompactString(subject),
            JsonElementHelper.ToCompactString(expectation),
            typeof(string));

        nestedValidator.RecursivelyAssertEquality(newComparands, context);

        return EquivalencyResult.AssertionCompleted;
    }
}
