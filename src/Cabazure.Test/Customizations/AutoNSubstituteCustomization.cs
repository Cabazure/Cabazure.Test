using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace Cabazure.Test.Customizations;

/// <summary>
/// An AutoFixture customization that configures the fixture to automatically
/// create NSubstitute substitutes for interfaces and abstract classes.
/// </summary>
/// <remarks>
/// Apply this customization to an <see cref="IFixture"/> to enable automatic
/// mocking of dependencies when using <see cref="FixtureFactory"/>.
/// </remarks>
public sealed class AutoNSubstituteCustomization : ICustomization
{
    /// <inheritdoc />
    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        fixture.Customize(new AutoFixture.AutoNSubstitute.AutoNSubstituteCustomization
        {
            ConfigureMembers = true,
            GenerateDelegates = true,
        });
    }
}
