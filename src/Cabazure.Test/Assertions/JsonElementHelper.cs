using System.Text;
using System.Text.Json;

namespace Cabazure.Test;

/// <summary>
/// Internal helpers for working with <see cref="JsonElement"/> values without reflection.
/// </summary>
internal static class JsonElementHelper
{
    /// <summary>
    /// Serializes a <see cref="JsonElement"/> to a compact JSON string using
    /// <see cref="Utf8JsonWriter"/> — no <c>JsonSerializer</c>, no reflection.
    /// The output is always compact (non-indented) regardless of the source's original formatting,
    /// making it suitable for whitespace-normalized equivalence comparisons.
    /// </summary>
    internal static string ToCompactString(JsonElement element)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        element.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
