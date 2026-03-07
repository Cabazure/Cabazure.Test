using System.Collections;
using AutoFixture;
using Cabazure.Test.Customizations;

namespace Cabazure.Test;

/// <summary>
/// An ordered, thread-safe collection of <see cref="ICustomization"/> instances applied
/// to every <see cref="IFixture"/> created by <see cref="FixtureFactory"/>.
/// </summary>
/// <remarks>
/// <para>
/// The collection is pre-seeded with <see cref="AutoNSubstituteCustomization"/>,
/// <see cref="RecursionCustomization"/>, and <see cref="ImmutableCollectionCustomization"/>
/// as the first three entries. Customizations are applied in the order they appear in
/// the collection.
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
}
