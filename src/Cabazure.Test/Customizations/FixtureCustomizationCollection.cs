using System.Collections;
using AutoFixture;
using AutoFixture.Kernel;

namespace Cabazure.Test.Customizations;

/// <summary>
/// An ordered, thread-safe collection of <see cref="ICustomization"/> instances applied
/// to every <see cref="IFixture"/> created by <see cref="FixtureFactory"/>.
/// </summary>
/// <remarks>
/// <para>
/// The collection is pre-seeded with <see cref="AutoNSubstituteCustomization"/>,
/// <see cref="RecursionCustomization"/>, <see cref="ImmutableCollectionCustomization"/>,
/// <see cref="DateOnlyTimeOnlyCustomization"/>, <see cref="CancellationTokenCustomization"/>,
/// <see cref="JsonElementCustomization"/>, and <see cref="JsonSerializerOptionsCustomization"/>
/// as the first seven entries.
/// Customizations are applied in the order they appear in the collection.
/// </para>
/// <para>
/// Register project-wide customizations from a <c>[ModuleInitializer]</c>:
/// </para>
/// <example>
/// <code>
/// internal static class TestAssemblyInitializer
/// {
///     [ModuleInitializer]
///     public static void Initialize()
///         => FixtureFactory.Customizations.Add(new MyDomainCustomization());
/// }
/// </code>
/// </example>
/// </remarks>
public sealed class FixtureCustomizationCollection : IEnumerable<ICustomization>
{
    private readonly List<ICustomization> customizations;
    private readonly object syncLock = new();

    internal FixtureCustomizationCollection()
    {
        customizations =
        [
            new AutoNSubstituteCustomization(),
            new RecursionCustomization(),
            new ImmutableCollectionCustomization(),
            new DateOnlyTimeOnlyCustomization(),
            new CancellationTokenCustomization(),
            new JsonElementCustomization(),
            new JsonSerializerOptionsCustomization(),
        ];
    }

    /// <summary>Gets the number of customizations currently in the collection.</summary>
    public int Count
    {
        get
        {
            lock (syncLock)
            {
                return customizations.Count;
            }
        }
    }

    /// <summary>
    /// Appends a customization to the end of the collection.
    /// </summary>
    /// <param name="customization">The customization to add. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="customization"/> is <see langword="null"/>.
    /// </exception>
    public void Add(ICustomization customization)
    {
        ArgumentNullException.ThrowIfNull(customization);
        lock (syncLock)
        {
            customizations.Add(customization);
        }
    }

    /// <summary>
    /// Registers a factory function for creating instances of <typeparamref name="T"/>.
    /// This is the simplest way to customize how a specific type is created inline.
    /// </summary>
    /// <typeparam name="T">The type to customize.</typeparam>
    /// <param name="factory">
    /// A function that receives an <see cref="IFixture"/> and returns an instance of <typeparamref name="T"/>.
    /// Must not be <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="factory"/> is <see langword="null"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// FixtureFactory.Customizations.Add&lt;DateOnly&gt;(f => DateOnly.FromDateTime(f.Create&lt;DateTime&gt;()));
    /// </code>
    /// </example>
    public void Add<T>(Func<IFixture, T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Add(new TypeCustomization<T>(factory));
    }

    /// <summary>
    /// Wraps and registers an <see cref="ISpecimenBuilder"/> as a customization.
    /// This is for power users who need full control over specimen creation logic.
    /// </summary>
    /// <param name="builder">
    /// The specimen builder to add. Must not be <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> is <see langword="null"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// FixtureFactory.Customizations.Add(new MyAdvancedBuilder());
    /// </code>
    /// </example>
    public void Add(ISpecimenBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        Add(new SpecimenBuilderCustomizationWrapper(builder));
    }

    /// <summary>
    /// Removes the first occurrence of <paramref name="customization"/> from the collection.
    /// </summary>
    /// <param name="customization">The instance to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the item was found and removed; <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="customization"/> is <see langword="null"/>.
    /// </exception>
    public bool Remove(ICustomization customization)
    {
        ArgumentNullException.ThrowIfNull(customization);
        lock (syncLock)
        {
            return customizations.Remove(customization);
        }
    }

    /// <summary>
    /// Removes the first customization in the collection that is an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The customization type to remove.</typeparam>
    /// <returns>
    /// <see langword="true"/> if a matching item was found and removed; <see langword="false"/> otherwise.
    /// </returns>
    public bool Remove<T>() where T : ICustomization
    {
        lock (syncLock)
        {
            var index = customizations.FindIndex(c => c is T);
            if (index < 0)
            {
                return false;
            }

            customizations.RemoveAt(index);
            return true;
        }
    }

    /// <summary>Removes all customizations from the collection.</summary>
    public void Clear()
    {
        lock (syncLock)
        {
            customizations.Clear();
        }
    }

    /// <summary>
    /// Returns an enumerator over a snapshot of the current collection contents.
    /// Modifications made after the enumerator is created are not reflected.
    /// </summary>
    /// <returns>An enumerator over a snapshot of the customizations.</returns>
    public IEnumerator<ICustomization> GetEnumerator()
    {
        List<ICustomization> snapshot;
        lock (syncLock)
        {
            snapshot = [..customizations];
        }

        return snapshot.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class SpecimenBuilderCustomizationWrapper : ICustomization
    {
        private readonly ISpecimenBuilder builder;

        public SpecimenBuilderCustomizationWrapper(ISpecimenBuilder builder)
        {
            this.builder = builder;
        }

        public void Customize(IFixture fixture) => fixture.Customizations.Add(builder);
    }
}
