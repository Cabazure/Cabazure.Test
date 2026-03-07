using System.Reflection;
using System.Runtime.CompilerServices;
using AutoFixture;
using Cabazure.Test.Attributes;
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
    /// Creates a new <see cref="IFixture"/> with <see cref="AutoNSubstituteCustomization"/> applied.
    /// </summary>
    /// <returns>A configured <see cref="IFixture"/>.</returns>
    public static IFixture Create()
        => Create([]);

    /// <summary>
    /// Creates a new <see cref="IFixture"/> with <see cref="AutoNSubstituteCustomization"/> applied,
    /// followed by any additional <paramref name="customizations"/> in order.
    /// </summary>
    /// <param name="customizations">
    /// Additional customizations to apply after <see cref="AutoNSubstituteCustomization"/>.
    /// </param>
    /// <returns>A configured <see cref="IFixture"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="customizations"/> is <see langword="null"/>.
    /// </exception>
    public static IFixture Create(params ICustomization[] customizations)
    {
        ArgumentNullException.ThrowIfNull(customizations);
        var fixture = new Fixture();
        fixture.Customize(new AutoNSubstituteCustomization());
        foreach (var customization in customizations)
        {
            fixture.Customize(customization);
        }
        return fixture;
    }

    /// <summary>
    /// Creates a new <see cref="IFixture"/> for the given test method, applying all registered
    /// customizations in priority order:
    /// <list type="number">
    ///   <item><description><see cref="AutoNSubstituteCustomization"/> — always first.</description></item>
    ///   <item><description>Project-wide registrations from <see cref="SutFixtureCustomizations"/>.</description></item>
    ///   <item><description><see cref="CustomizeWithAttribute"/> instances on the test method.</description></item>
    ///   <item><description><see cref="CustomizeWithAttribute"/> instances on the declaring class.</description></item>
    /// </list>
    /// </summary>
    /// <param name="testMethod">The theory method for which the fixture is being created.</param>
    /// <returns>A fully configured <see cref="IFixture"/>.</returns>
    internal static IFixture Create(MethodInfo testMethod)
    {
        var declaringType = testMethod.DeclaringType;
        if (declaringType is not null)
        {
            RuntimeHelpers.RunClassConstructor(declaringType.TypeHandle);
        }

        var fixture = new Fixture();
        fixture.Customize(new AutoNSubstituteCustomization());

        foreach (var customization in SutFixtureCustomizations.All)
        {
            fixture.Customize(customization);
        }

        foreach (var attr in testMethod.GetCustomAttributes<CustomizeWithAttribute>())
        {
            fixture.Customize(attr.Instantiate());
        }

        if (declaringType is not null)
        {
            foreach (var attr in declaringType.GetCustomAttributes<CustomizeWithAttribute>())
            {
                fixture.Customize(attr.Instantiate());
            }
        }

        return fixture;
    }
}
