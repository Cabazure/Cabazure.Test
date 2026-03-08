using System.Text.Json;
using AutoFixture;

namespace Cabazure.Test.Customizations;

/// <summary>
/// An AutoFixture customization that provides a valid <see cref="JsonSerializerOptions"/>
/// instance for use in unit tests.
/// </summary>
/// <remarks>
/// <para>
/// AutoFixture cannot construct <see cref="JsonSerializerOptions"/> by default because it
/// tries to set the <see cref="JsonSerializerOptions.IndentCharacter"/> property (added in
/// .NET 8) to a randomly generated <see cref="char"/>. That property only accepts
/// <c>' '</c> (space) or <c>'\t'</c> (horizontal tab), causing an
/// <see cref="ArgumentOutOfRangeException"/> with the message
/// <c>"Supported indentation characters are space and horizontal tab."</c>.
/// </para>
/// <para>
/// This customization replaces that behaviour with <c>new JsonSerializerOptions()</c>, which
/// produces a valid, usable options instance with all defaults applied.
/// </para>
/// <para>
/// To use custom settings project-wide, remove this customization and register a replacement
/// from a <c>[ModuleInitializer]</c>:
/// </para>
/// <code>
/// FixtureFactory.Customizations.Remove&lt;JsonSerializerOptionsCustomization&gt;();
/// FixtureFactory.Customizations.Add(_ => new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
/// </code>
/// <para>
/// To override for a single fixture, use <see cref="IFixture.Inject{T}(T)"/>:
/// </para>
/// <code>
/// fixture.Inject(new JsonSerializerOptions(JsonSerializerDefaults.Web));
/// </code>
/// </remarks>
public sealed class JsonSerializerOptionsCustomization : TypeCustomization<JsonSerializerOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSerializerOptionsCustomization"/> class.
    /// </summary>
    public JsonSerializerOptionsCustomization()
        : base(_ => new JsonSerializerOptions())
    {
    }
}
