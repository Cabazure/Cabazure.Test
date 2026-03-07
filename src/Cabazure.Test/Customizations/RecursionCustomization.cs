using AutoFixture;

namespace Cabazure.Test.Customizations;

/// <summary>
/// An AutoFixture customization that configures the fixture to omit recursive
/// references instead of throwing when it encounters recursive object graphs.
/// </summary>
/// <remarks>
/// By default AutoFixture throws a <see cref="ObjectCreationException"/> when it
/// detects a recursive type graph. Apply this customization to suppress that
/// behaviour and instead omit the recursive property (leave it <see langword="null"/>).
/// </remarks>
public sealed class RecursionCustomization : ICustomization
{
    /// <inheritdoc />
    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        fixture.Behaviors
            .OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
}
