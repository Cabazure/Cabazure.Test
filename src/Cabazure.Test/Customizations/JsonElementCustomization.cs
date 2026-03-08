using System.IO;
using System.Text.Json;
using AutoFixture;

namespace Cabazure.Test.Customizations;

/// <summary>
/// An AutoFixture customization that enables the fixture to create instances of
/// <see cref="JsonElement"/>.
/// </summary>
/// <remarks>
/// <see cref="JsonElement"/> cannot be constructed by AutoFixture by default because
/// its constructors require a <c>ref Utf8JsonReader</c> parameter.
/// <para>
/// Three factory modes are available:
/// <list type="bullet">
///   <item><description>
///     Default (no args) — produces a JSON string via <see cref="Utf8JsonWriter"/>;
///     works regardless of <c>JsonSerializerIsReflectionEnabledByDefault</c>.
///   </description></item>
///   <item><description>
///     <c>Func&lt;IFixture, string&gt;</c> — caller returns a raw JSON string;
///     parsing and cloning are handled automatically; no reflection required.
///   </description></item>
///   <item><description>
///     <c>Func&lt;IFixture, JsonElement&gt;</c> — caller produces the element
///     directly with full control over construction.
///   </description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class JsonElementCustomization : TypeCustomization<JsonElement>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonElementCustomization"/> class
    /// that produces a <see cref="JsonElement"/> representing a randomly generated JSON string.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Utf8JsonWriter"/> directly so that it works regardless of whether
    /// reflection-based JSON serialization is enabled in the test project.
    /// </remarks>
    public JsonElementCustomization()
        : base(f =>
        {
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer))
                writer.WriteStringValue(f.Create<string>());
            return JsonDocument.Parse(buffer.ToArray()).RootElement.Clone();
        })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonElementCustomization"/> class
    /// with a factory that returns a raw JSON string.
    /// </summary>
    /// <param name="jsonFactory">
    /// A function that receives an <see cref="IFixture"/> and returns a valid JSON string.
    /// The string is parsed into a <see cref="JsonElement"/> and cloned automatically —
    /// no reflection-based JSON serialization is required.
    /// </param>
    /// <example>
    /// <code>
    /// // JSON object with random key/value
    /// new JsonElementCustomization(
    ///     f => $"{{\"{f.Create&lt;string&gt;()}\": \"{f.Create&lt;string&gt;()}\"}}")
    ///
    /// // JSON number
    /// new JsonElementCustomization(
    ///     f => f.Create&lt;int&gt;().ToString())
    /// </code>
    /// </example>
    public JsonElementCustomization(Func<IFixture, string> jsonFactory)
        : base(f => Parse(jsonFactory(f)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonElementCustomization"/> class
    /// with a factory that produces a <see cref="JsonElement"/> directly.
    /// </summary>
    /// <param name="elementFactory">
    /// A function that receives an <see cref="IFixture"/> and returns the
    /// <see cref="JsonElement"/> to register. The factory has complete control
    /// over how the element is constructed.
    /// </param>
    /// <example>
    /// <code>
    /// // JSON object built with Utf8JsonWriter (no reflection)
    /// new JsonElementCustomization(f =>
    /// {
    ///     using var buffer = new MemoryStream();
    ///     using (var writer = new Utf8JsonWriter(buffer))
    ///     {
    ///         writer.WriteStartObject();
    ///         writer.WriteString(f.Create&lt;string&gt;(), f.Create&lt;string&gt;());
    ///         writer.WriteEndObject();
    ///     }
    ///     return JsonDocument.Parse(buffer.ToArray()).RootElement.Clone();
    /// })
    /// </code>
    /// </example>
    public JsonElementCustomization(Func<IFixture, JsonElement> elementFactory)
        : base(elementFactory)
    {
    }

    private static JsonElement Parse(string json)
        => JsonDocument.Parse(json).RootElement.Clone();
}
