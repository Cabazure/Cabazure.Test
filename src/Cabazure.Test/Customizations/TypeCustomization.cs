using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;

namespace Cabazure.Test.Customizations;

/// <summary>
/// A customization that registers a factory function for creating instances of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type to customize.</typeparam>
/// <remarks>
/// <para>
/// This customization allows you to control how <see cref="IFixture"/> creates instances of a specific type
/// by providing a factory function that receives the <see cref="IFixture"/> instance itself. This enables
/// you to leverage the fixture's full API within your factory, including <c>Create&lt;T&gt;()</c>,
/// <c>Build&lt;T&gt;().With(...).Create()</c>, and other fluent operations.
/// </para>
/// <para>
/// The factory is invoked whenever AutoFixture needs to create an instance of <typeparamref name="T"/>,
/// whether requested directly via <c>Create&lt;T&gt;()</c>, as a constructor parameter, property, or field.
/// </para>
/// </remarks>
/// <example>
/// <para><strong>Direct instantiation for inline use:</strong></para>
/// <code>
/// var fixture = FixtureFactory.Create(
///     new TypeCustomization&lt;JsonElement&gt;(f =>
///     {
///         var json = $"{{\"id\":\"{f.Create&lt;Guid&gt;()}\"}}";
///         return JsonDocument.Parse(json).RootElement.Clone();
///     }));
///
/// var element = fixture.Create&lt;JsonElement&gt;();
/// </code>
/// <para><strong>Subclassing for reusable customizations:</strong></para>
/// <code>
/// public sealed class DateOnlyCustomization : TypeCustomization&lt;DateOnly&gt;
/// {
///     public DateOnlyCustomization()
///         : base(f => DateOnly.FromDateTime(f.Create&lt;DateTime&gt;()))
///     {
///     }
/// }
///
/// FixtureFactory.Customizations.Add(new DateOnlyCustomization());
/// </code>
/// </example>
public sealed class TypeCustomization<T> : ICustomization
{
    private readonly Func<IFixture, T> factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeCustomization{T}"/> class
    /// with the specified factory function.
    /// </summary>
    /// <param name="factory">
    /// A function that receives an <see cref="IFixture"/> and returns an instance of <typeparamref name="T"/>.
    /// Must not be <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="factory"/> is <see langword="null"/>.
    /// </exception>
    public TypeCustomization(Func<IFixture, T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        this.factory = factory;
    }

    /// <inheritdoc/>
    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        fixture.Customizations.Add(new DelegateBuilder(fixture, factory));
    }

    private sealed class DelegateBuilder : ISpecimenBuilder
    {
        private readonly IFixture fixture;
        private readonly Func<IFixture, T> factory;

        public DelegateBuilder(IFixture fixture, Func<IFixture, T> factory)
        {
            this.fixture = fixture;
            this.factory = factory;
        }

        public object Create(object request, ISpecimenContext context)
        {
            var requestType = GetRequestType(request);
            if (requestType != typeof(T))
            {
                return new NoSpecimen();
            }

            return factory(fixture)!;
        }

        private static Type? GetRequestType(object request) => request switch
        {
            ParameterInfo pi => pi.ParameterType,
            PropertyInfo pi => pi.PropertyType,
            FieldInfo fi => fi.FieldType,
            Type t => t,
            _ => null,
        };
    }
}
