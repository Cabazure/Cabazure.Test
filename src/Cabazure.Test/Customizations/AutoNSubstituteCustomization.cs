using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace Cabazure.Test.Customizations;

/// <summary>
/// An AutoFixture customization that configures the fixture to automatically
/// create NSubstitute substitutes for interfaces and abstract classes.
/// </summary>
/// <remarks>
/// Substitutes follow standard NSubstitute behavior: un-setup method calls
/// return <see langword="null"/> for reference types, <c>0</c> for numeric types,
/// <see langword="false"/> for <see cref="bool"/>, and a completed
/// <see cref="System.Threading.Tasks.Task"/> for async methods. Use
/// <c>.Returns()</c> to configure explicit return values.
/// </remarks>
public sealed class AutoNSubstituteCustomization : ICustomization
{
    /// <inheritdoc />
    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        fixture.Customize(new AutoFixture.AutoNSubstitute.AutoNSubstituteCustomization
        {
            ConfigureMembers = false,
            GenerateDelegates = true,
        });
    }
}
