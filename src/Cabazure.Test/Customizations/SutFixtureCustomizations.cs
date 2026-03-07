using AutoFixture;

namespace Cabazure.Test.Customizations;

/// <summary>
/// Global registry for project-wide <see cref="ICustomization"/> instances that are applied
/// to every <see cref="Fixture.SutFixture"/> created by the Cabazure.Test data attributes.
/// </summary>
/// <remarks>
/// <para>
/// Register customizations from a <c>[ModuleInitializer]</c> in the test project so they
/// are applied once per assembly load, before any tests run. Customizations registered here
/// are applied after <see cref="AutoNSubstituteCustomization"/> but before any
/// <see cref="Attributes.CustomizeWithAttribute"/> overrides on individual tests.
/// </para>
/// <example>
/// <code>
/// internal static class TestAssemblyInitializer
/// {
///     [ModuleInitializer]
///     public static void Initialize()
///         => SutFixtureCustomizations.Add(new MyDomainCustomization());
/// }
/// </code>
/// </example>
/// </remarks>
public static class SutFixtureCustomizations
{
    private static readonly List<ICustomization> _customizations = [];
    private static readonly object _lock = new();

    /// <summary>
    /// Registers a project-wide <see cref="ICustomization"/> that will be applied to every
    /// <see cref="Fixture.SutFixture"/> created by the Cabazure.Test data attributes.
    /// </summary>
    /// <param name="customization">The customization to register. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="customization"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// This method is thread-safe. Call it from a <c>[ModuleInitializer]</c> to ensure the
    /// customization is registered before any tests execute.
    /// </remarks>
    public static void Add(ICustomization customization)
    {
        ArgumentNullException.ThrowIfNull(customization);
        lock (_lock)
        {
            _customizations.Add(customization);
        }
    }

    /// <summary>Gets all registered project-wide customizations.</summary>
    internal static IReadOnlyList<ICustomization> All
    {
        get
        {
            lock (_lock)
            {
                return [.._customizations];
            }
        }
    }
}
