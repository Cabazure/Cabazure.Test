using System.Reflection;
using System.Text.Json;
using AutoFixture;
using AutoFixture.Kernel;

namespace Cabazure.Test.Customizations;

/// <summary>
/// An AutoFixture customization that enables the fixture to create instances of
/// <see cref="JsonElement"/>.
/// </summary>
/// <remarks>
/// <see cref="JsonElement"/> cannot be constructed by AutoFixture by default because
/// its constructors require a <c>ref Utf8JsonReader</c> parameter. This customization
/// creates a <see cref="JsonElement"/> representing a JSON object with a randomly
/// generated key/value string pair. The element is cloned so it is not tied to the
/// lifetime of its backing <see cref="JsonDocument"/>.
/// </remarks>
public sealed class JsonElementCustomization : ICustomization
{
    /// <inheritdoc />
    public void Customize(IFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        fixture.Customizations.Add(new JsonElementBuilder());
    }

    private sealed class JsonElementBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (GetRequestType(request) != typeof(JsonElement))
            {
                return new NoSpecimen();
            }

            var key = (string)context.Resolve(typeof(string));
            var value = (string)context.Resolve(typeof(string));
            var json = $"{{\"{key}\":\"{value}\"}}";

            return JsonDocument.Parse(json).RootElement.Clone();
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
