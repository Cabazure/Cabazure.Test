using System.Collections.Immutable;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;

namespace Cabazure.Test.Customizations;

/// <summary>
/// An AutoFixture customization that enables the fixture to create instances of
/// immutable collection types from <see cref="System.Collections.Immutable"/>,
/// including <see cref="ImmutableArray{T}"/>, <see cref="ImmutableList{T}"/>,
/// <see cref="ImmutableDictionary{TKey, TValue}"/>, <see cref="ImmutableHashSet{T}"/>,
/// <see cref="ImmutableSortedSet{T}"/>, <see cref="ImmutableSortedDictionary{TKey, TValue}"/>,
/// <see cref="ImmutableQueue{T}"/>, and <see cref="ImmutableStack{T}"/>.
/// </summary>
public sealed class ImmutableCollectionCustomization : ICustomization
{
    /// <inheritdoc />
    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableArray<>),
                typeof(List<>),
                o => ImmutableArray.ToImmutableArray(o)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableList<>),
                typeof(List<>),
                o => ImmutableList.ToImmutableList(o)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableDictionary<,>),
                typeof(Dictionary<,>),
                o => ImmutableDictionary.ToImmutableDictionary(o)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableHashSet<>),
                typeof(HashSet<>),
                o => ImmutableHashSet.ToImmutableHashSet(o)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableSortedSet<>),
                typeof(SortedSet<>),
                o => ImmutableSortedSet.ToImmutableSortedSet(o)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableSortedDictionary<,>),
                typeof(SortedDictionary<,>),
                o => ImmutableSortedDictionary.ToImmutableSortedDictionary(o)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableQueue<>),
                typeof(List<>),
                o => ImmutableQueue.CreateRange(o)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableStack<>),
                typeof(List<>),
                o => ImmutableStack.CreateRange(o)));
    }

    private sealed class ImmutableCollectionBuilder(
        Type immutableType,
        Type underlyingType,
        Func<dynamic, object> converter)
        : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (GetRequestType(request) is { } type
                && type.IsGenericType
                && type.GetGenericTypeDefinition() == immutableType
                && type.GetGenericArguments() is { Length: > 0 } args)
            {
                var listType = underlyingType.MakeGenericType(args);
                dynamic list = context.Resolve(listType);

                return converter.Invoke(list);
            }

            return new NoSpecimen();
        }

        private static Type? GetRequestType(object request)
            => request switch
            {
                ParameterInfo pi => pi.ParameterType,
                PropertyInfo pi => pi.PropertyType,
                FieldInfo fi => fi.FieldType,
                Type t => t,
                _ => null,
            };
    }
}
