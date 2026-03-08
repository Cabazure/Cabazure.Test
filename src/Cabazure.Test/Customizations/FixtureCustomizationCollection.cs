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
    private readonly object syncLock = new();
    private volatile ICustomization[] items;

    internal FixtureCustomizationCollection()
    {
        items = new ICustomization[]
        {
            new AutoNSubstituteCustomization(),
            new RecursionCustomization(),
            new ImmutableCollectionCustomization(),
#if NET6_0_OR_GREATER
            new DateOnlyTimeOnlyCustomization(),
#endif
            new CancellationTokenCustomization(),
            new JsonElementCustomization(),
            new JsonSerializerOptionsCustomization(),
        };
    }

    /// <summary>Gets the number of customizations currently in the collection.</summary>
    public int Count => items.Length;

    /// <summary>
    /// Appends a customization to the end of the collection.
    /// </summary>
    /// <param name="customization">The customization to add. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="customization"/> is <see langword="null"/>.
    /// </exception>
    public void Add(ICustomization customization)
    {
        if (customization is null) throw new ArgumentNullException(nameof(customization));
        lock (syncLock)
            items = items.Concat(new[] { customization }).ToArray();
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
        if (factory is null) throw new ArgumentNullException(nameof(factory));
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
        if (builder is null) throw new ArgumentNullException(nameof(builder));
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
        if (customization is null) throw new ArgumentNullException(nameof(customization));
        lock (syncLock)
        {
            var current = items;
            var idx = Array.IndexOf(current, customization);
            if (idx < 0) return false;
            var next = new ICustomization[current.Length - 1];
            Array.Copy(current, 0, next, 0, idx);
            Array.Copy(current, idx + 1, next, idx, current.Length - idx - 1);
            items = next;
            return true;
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
            var current = items;
            var idx = Array.FindIndex(current, c => c is T);
            if (idx < 0) return false;
            var next = new ICustomization[current.Length - 1];
            Array.Copy(current, 0, next, 0, idx);
            Array.Copy(current, idx + 1, next, idx, current.Length - idx - 1);
            items = next;
            return true;
        }
    }

    /// <summary>Removes all customizations from the collection.</summary>
    public void Clear()
    {
        lock (syncLock)
            items = Array.Empty<ICustomization>();
    }

    /// <summary>
    /// Returns an enumerator over a snapshot of the current collection contents.
    /// Modifications made after the enumerator is created are not reflected.
    /// </summary>
    /// <returns>An enumerator over a snapshot of the customizations.</returns>
    public IEnumerator<ICustomization> GetEnumerator()
        => ((IEnumerable<ICustomization>)items).GetEnumerator();

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
