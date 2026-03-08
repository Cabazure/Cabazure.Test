using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoFixture;
using Cabazure.Test.Customizations;

namespace Cabazure.Test;

/// <summary>
/// Factory for creating AutoFixture <see cref="IFixture"/> instances configured
/// for use with Cabazure.Test — with automatic NSubstitute substitution for
/// interfaces and abstract classes.
/// </summary>
/// <remarks>
/// <para>
/// Use this factory directly in <c>[Fact]</c> tests where you need a configured
/// fixture without theory parameters. For theory tests, prefer
/// <see cref="AutoNSubstituteDataAttribute"/> and its variants which call this
/// factory automatically.
/// </para>
/// <example>
/// <code>
/// [Fact]
/// public void MyTest()
/// {
///     var fixture = FixtureFactory.Create();
///     var sut = fixture.Create&lt;MyService&gt;();
///     var dep = fixture.Freeze&lt;IMyDependency&gt;();
/// }
/// </code>
/// </example>
/// </remarks>
public static class FixtureFactory
{
    /// <summary>
    /// Gets the collection of customizations applied to every <see cref="IFixture"/>
    /// created by this factory. Add project-wide customizations here from a
    /// <c>[ModuleInitializer]</c>. The collection is pre-seeded with
    /// <see cref="AutoFixture.AutoNSubstitute.AutoNSubstituteCustomization"/>.
    /// </summary>
    /// <remarks>
    /// Customizations in this collection are applied before any per-test
    /// <see cref="CustomizeWithAttribute"/> overrides.
    /// </remarks>
    public static FixtureCustomizationCollection Customizations { get; } = new();

    private static readonly ConcurrentDictionary<RuntimeTypeHandle, byte> InitializedTypes = new();
    private static readonly ConcurrentDictionary<MethodInfo, ICustomization[]> MethodCustomizations = new();
    private static readonly ConcurrentDictionary<Type, ICustomization[]> TypeCustomizations = new();

    /// <summary>
    /// Creates a new <see cref="IFixture"/> with <see cref="AutoFixture.AutoNSubstitute.AutoNSubstituteCustomization"/> applied.
    /// </summary>
    /// <returns>A configured <see cref="IFixture"/>.</returns>
    public static IFixture Create()
        => Create([]);

    /// <summary>
    /// Creates a new <see cref="IFixture"/> with all <see cref="Customizations"/> applied,
    /// followed by any additional <paramref name="customizations"/> in order.
    /// </summary>
    /// <param name="customizations">
    /// Additional customizations to apply after those in <see cref="Customizations"/>.
    /// </param>
    /// <returns>A configured <see cref="IFixture"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="customizations"/> is <see langword="null"/>.
    /// </exception>
    public static IFixture Create(params ICustomization[] customizations)
    {
        if (customizations is null) throw new ArgumentNullException(nameof(customizations));
        var fixture = new Fixture();
        ApplyCustomizations(fixture, Customizations);
        ApplyCustomizations(fixture, customizations);
        return fixture;
    }

    /// <summary>
    /// Creates a new <see cref="IFixture"/> for the given test method, applying all registered
    /// customizations in priority order:
    /// <list type="number">
    ///   <item><description>All customizations from <see cref="Customizations"/> (including <see cref="AutoFixture.AutoNSubstitute.AutoNSubstituteCustomization"/> which is always first).</description></item>
    ///   <item><description><see cref="CustomizeWithAttribute"/> instances on the test method.</description></item>
    ///   <item><description><see cref="CustomizeWithAttribute"/> instances on the declaring class.</description></item>
    /// </list>
    /// </summary>
    /// <param name="testMethod">The theory method for which the fixture is being created.</param>
    /// <returns>A fully configured <see cref="IFixture"/>.</returns>
    internal static IFixture Create(MethodInfo testMethod)
    {
        var declaringType = testMethod.DeclaringType;
        if (declaringType is not null && InitializedTypes.TryAdd(declaringType.TypeHandle, 0))
            RuntimeHelpers.RunClassConstructor(declaringType.TypeHandle);

        var fixture = new Fixture();
        ApplyCustomizations(fixture, Customizations);

        foreach (var c in MethodCustomizations.GetOrAdd(testMethod,
            m => [.. m.GetCustomAttributes<CustomizeWithAttribute>().Select(a => a.Instantiate())]))
            fixture.Customize(c);

        if (declaringType is not null)
        {
            foreach (var c in TypeCustomizations.GetOrAdd(declaringType,
                t => [.. t.GetCustomAttributes<CustomizeWithAttribute>().Select(a => a.Instantiate())]))
                fixture.Customize(c);
        }

        return fixture;
    }

    private static void ApplyCustomizations(IFixture fixture, IEnumerable<ICustomization> customizations)
    {
        foreach (var customization in customizations)
            fixture.Customize(customization);
    }
}
