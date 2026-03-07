using AutoFixture;
using AutoFixture.Dsl;
using AutoFixture.Kernel;
using Cabazure.Test.Customizations;
using NSubstitute;

namespace Cabazure.Test.Fixture;

/// <summary>
/// A test fixture that combines AutoFixture and NSubstitute to provide
/// automatic creation and substitution of System Under Test (SUT) dependencies.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SutFixture"/> wraps an AutoFixture <see cref="IFixture"/> configured
/// with <see cref="AutoNSubstituteCustomization"/>, so that any unregistered interface
/// or abstract class dependency is automatically replaced with an NSubstitute substitute.
/// </para>
/// <para>
/// Use <see cref="Create{T}"/> to create instances with all dependencies auto-wired,
/// <see cref="Freeze{T}"/> to register a single shared instance, and
/// <see cref="Substitute{T}"/> to explicitly create a substitute.
/// </para>
/// </remarks>
public sealed class SutFixture
{
    private readonly IFixture fixture;

    /// <summary>
    /// Initializes a new instance of <see cref="SutFixture"/> with default configuration.
    /// </summary>
    public SutFixture()
        : this(new AutoNSubstituteCustomization())
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SutFixture"/> with the specified customizations.
    /// </summary>
    /// <param name="customizations">
    /// One or more customizations to apply to the underlying AutoFixture fixture.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="customizations"/> is <see langword="null"/>.
    /// </exception>
    public SutFixture(params ICustomization[] customizations)
    {
        ArgumentNullException.ThrowIfNull(customizations);
        fixture = new AutoFixture.Fixture();
        foreach (var customization in customizations)
        {
            fixture.Customize(customization);
        }
    }

    /// <summary>
    /// Creates an instance of <typeparamref name="T"/> with all unregistered dependencies
    /// automatically substituted via NSubstitute.
    /// </summary>
    /// <typeparam name="T">The type to create.</typeparam>
    /// <returns>A new instance of <typeparamref name="T"/>.</returns>
    public T Create<T>() => fixture.Create<T>();

    /// <summary>
    /// Creates a sequence of instances of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to create.</typeparam>
    /// <param name="count">The number of instances to create. Defaults to 3.</param>
    /// <returns>A sequence of <typeparamref name="T"/> instances.</returns>
    public IEnumerable<T> CreateMany<T>(int count = 3) => fixture.CreateMany<T>(count);

    /// <summary>
    /// Registers a single instance of <typeparamref name="T"/> so that all subsequent
    /// <see cref="Create{T}"/> calls that require a <typeparamref name="T"/> receive
    /// the same instance.
    /// </summary>
    /// <typeparam name="T">The type to freeze.</typeparam>
    /// <returns>
    /// The frozen instance. If <typeparamref name="T"/> is an interface or abstract class,
    /// the returned value is an NSubstitute substitute.
    /// </returns>
    public T Freeze<T>() where T : class => fixture.Freeze<T>();

    /// <summary>
    /// Registers the specified <paramref name="instance"/> so that all subsequent
    /// <see cref="Create{T}"/> calls that require a <typeparamref name="T"/> receive
    /// the same instance.
    /// </summary>
    /// <typeparam name="T">The type to freeze.</typeparam>
    /// <param name="instance">The instance to register.</param>
    /// <returns>The registered <paramref name="instance"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="instance"/> is <see langword="null"/>.
    /// </exception>
    public T Freeze<T>(T instance) where T : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        fixture.Inject(instance);
        return instance;
    }

    /// <summary>
    /// Configures AutoFixture customization for type <typeparamref name="T"/> using
    /// the provided composer transformation.
    /// </summary>
    /// <typeparam name="T">The type to customize.</typeparam>
    /// <param name="composerTransformation">
    /// A function that configures the composer for <typeparamref name="T"/>.
    /// </param>
    public void Customize<T>(
        Func<ICustomizationComposer<T>, ISpecimenBuilder> composerTransformation)
    {
        ArgumentNullException.ThrowIfNull(composerTransformation);
        fixture.Customize<T>(composerTransformation);
    }

    /// <summary>
    /// Creates an NSubstitute substitute for an interface or abstract class <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The interface or abstract class type to substitute.</typeparam>
    /// <returns>An NSubstitute substitute for <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// Unlike <see cref="Freeze{T}"/>, this method does NOT register the substitute with
    /// the fixture — it simply creates and returns a new substitute instance.
    /// </remarks>
    public T Substitute<T>() where T : class => NSubstitute.Substitute.For<T>();
}
