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
        if (fixture is null) throw new ArgumentNullException(nameof(fixture));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableArray<>),
                typeof(List<>),
                FindConverter(typeof(ImmutableArray), "ToImmutableArray", 1)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableList<>),
                typeof(List<>),
                FindConverter(typeof(ImmutableList), "ToImmutableList", 1)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableDictionary<,>),
                typeof(Dictionary<,>),
                FindConverter(typeof(ImmutableDictionary), "ToImmutableDictionary", 2)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableHashSet<>),
                typeof(HashSet<>),
                FindConverter(typeof(ImmutableHashSet), "ToImmutableHashSet", 1)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableSortedSet<>),
                typeof(SortedSet<>),
                FindConverter(typeof(ImmutableSortedSet), "ToImmutableSortedSet", 1)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableSortedDictionary<,>),
                typeof(SortedDictionary<,>),
                FindConverter(typeof(ImmutableSortedDictionary), "ToImmutableSortedDictionary", 2)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableQueue<>),
                typeof(List<>),
                FindConverter(typeof(ImmutableQueue), "CreateRange", 1)));

        fixture.Customizations.Add(
            new ImmutableCollectionBuilder(
                typeof(ImmutableStack<>),
                typeof(List<>),
                FindConverter(typeof(ImmutableStack), "CreateRange", 1)));
    }

    private static MethodInfo FindConverter(Type declaringType, string methodName, int typeArity)
    {
        var methods = declaringType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        return Array.Find(
            methods,
            m => m.Name == methodName
                && m.IsGenericMethodDefinition
                && m.GetGenericArguments().Length == typeArity
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType.IsGenericType
                && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?? throw new InvalidOperationException(
                $"Could not find {declaringType.Name}.{methodName} with {typeArity} type argument(s) and an IEnumerable<> parameter.");
    }

    private sealed class ImmutableCollectionBuilder(
        Type immutableType,
        Type underlyingType,
        MethodInfo converterMethod)
        : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (SpecimenRequestHelper.GetRequestType(request) is { } type
                && type.IsGenericType
                && type.GetGenericTypeDefinition() == immutableType
                && type.GetGenericArguments() is { Length: > 0 } args)
            {
                var listType = underlyingType.MakeGenericType(args);
                var list = context.Resolve(listType);
                return converterMethod.MakeGenericMethod(args).Invoke(null, new[] { list })!;
            }

            return new NoSpecimen();
        }
    }
}
