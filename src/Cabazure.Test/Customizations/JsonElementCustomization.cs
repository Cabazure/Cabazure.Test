using System.Text.Json;
using AutoFixture;

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
public sealed class JsonElementCustomization : TypeCustomization<JsonElement>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonElementCustomization"/> class.
    /// </summary>
    public JsonElementCustomization()
        : base(f =>
        {
            var json = $"{{\"{f.Create<string>()}\":\"{f.Create<string>()}\"}}";
            return JsonDocument.Parse(json).RootElement.Clone();
        })
    {
    }
}
