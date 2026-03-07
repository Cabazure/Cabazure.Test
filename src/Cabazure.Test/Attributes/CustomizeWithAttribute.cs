using AutoFixture;

namespace Cabazure.Test.Attributes;

/// <summary>
/// Applies a per-test or per-class <see cref="ICustomization"/> to the <see cref="Fixture.SutFixture"/>
/// created for that test method.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CustomizationType"/> must implement <see cref="ICustomization"/> and expose
/// a public parameterless constructor so the framework can instantiate it at test discovery time.
/// </para>
/// <para>
/// Multiple attributes may be stacked on the same method or class; they are applied in
/// declaration order after any project-wide customizations registered via
/// <see cref="Customizations.SutFixtureCustomizations"/>.
/// </para>
/// <example>
/// <code>
/// [CustomizeWith(typeof(MyProjectCustomization))]
/// public class MyTests
/// {
///     [Theory, AutoNSubstituteData]
///     [CustomizeWith(typeof(MyTestCustomization))]
///     public void Test(IMyService service) { ... }
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class CustomizeWithAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="CustomizeWithAttribute"/>.
    /// </summary>
    /// <param name="customizationType">
    /// The type that implements <see cref="ICustomization"/> to apply. Must have a public
    /// parameterless constructor.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="customizationType"/> is <see langword="null"/>.
    /// </exception>
    public CustomizeWithAttribute(Type customizationType)
    {
        ArgumentNullException.ThrowIfNull(customizationType);
        CustomizationType = customizationType;
    }

    /// <summary>Gets the customization type to apply.</summary>
    public Type CustomizationType { get; }

    /// <summary>
    /// Instantiates and returns the <see cref="ICustomization"/> described by this attribute.
    /// </summary>
    /// <returns>A new instance of <see cref="CustomizationType"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="CustomizationType"/> does not implement <see cref="ICustomization"/>
    /// or does not have a public parameterless constructor.
    /// </exception>
    internal ICustomization Instantiate()
    {
        if (!typeof(ICustomization).IsAssignableFrom(CustomizationType))
        {
            throw new InvalidOperationException(
                $"Type '{CustomizationType.FullName}' does not implement '{typeof(ICustomization).FullName}'. " +
                $"Types passed to {nameof(CustomizeWithAttribute)} must implement ICustomization.");
        }

        if (CustomizationType.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new InvalidOperationException(
                $"Type '{CustomizationType.FullName}' does not have a public parameterless constructor. " +
                $"Types passed to {nameof(CustomizeWithAttribute)} must be default-constructible.");
        }

        return (ICustomization)Activator.CreateInstance(CustomizationType)!;
    }
}
