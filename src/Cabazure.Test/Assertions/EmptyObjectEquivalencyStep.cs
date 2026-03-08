using FluentAssertions.Equivalency;

namespace Cabazure.Test;

/// <summary>
/// A FluentAssertions <see cref="IEquivalencyStep"/> that allows <c>BeEquivalentTo</c> to succeed
/// when comparing instances of a type that has no public properties or fields.
/// </summary>
/// <remarks>
/// <para>
/// FluentAssertions 7.x throws <see cref="InvalidOperationException"/>
/// ("No members were found for comparison…") from <c>StructuralEqualityEquivalencyStep</c> any time
/// the root-level object graph has zero public instance members to compare. This is a common problem
/// when testing serialisation round-trips across many DTO types, some of which are marker/empty types.
/// </para>
/// <para>
/// This step intercepts the comparison pipeline <em>before</em> the structural step. If the
/// expectation type has zero public instance properties and zero public instance fields, the two
/// instances are considered trivially equivalent (there is nothing that can differ) and the assertion
/// is immediately completed. All other types pass through to FluentAssertions' normal pipeline.
/// </para>
/// <para>
/// Register per-call:
/// <code>
/// result.Should().BeEquivalentTo(expected, opts => opts.AllowingEmptyObjects());
/// </code>
/// </para>
/// <para>
/// Register globally (affects all equivalency assertions in the test session):
/// <code>
/// AssertionOptions.AssertEquivalencyUsing(opts => opts.AllowingEmptyObjects());
/// </code>
/// </para>
/// </remarks>
public sealed class EmptyObjectEquivalencyStep : IEquivalencyStep
{
    /// <inheritdoc cref="IEquivalencyStep.Handle"/>
    /// <summary>
    /// Completes the assertion immediately if the expectation type has no public instance members;
    /// otherwise passes control to the next step in the pipeline.
    /// </summary>
    /// <param name="comparands">The subject and expectation values to compare.</param>
    /// <param name="context">Context about the current node in the object graph.</param>
    /// <param name="nestedValidator">
    /// The validator used to recursively assert nested members (not used by this step).
    /// </param>
    /// <returns>
    /// <see cref="EquivalencyResult.AssertionCompleted"/> if the expectation type has no public
    /// instance properties and no public instance fields (the assertion trivially passes);
    /// <see cref="EquivalencyResult.ContinueWithNext"/> otherwise.
    /// </returns>
    public EquivalencyResult Handle(
        Comparands comparands,
        IEquivalencyValidationContext context,
        IEquivalencyValidator nestedValidator)
    {
        var type = comparands.Expectation?.GetType();
        if (type is null)
        {
            return EquivalencyResult.ContinueWithNext;
        }

        var hasProperties = type
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Length > 0;

        var hasFields = type
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Length > 0;

        if (hasProperties || hasFields)
        {
            return EquivalencyResult.ContinueWithNext;
        }

        return EquivalencyResult.AssertionCompleted;
    }
}
