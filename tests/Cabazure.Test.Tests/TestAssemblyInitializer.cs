using System.Runtime.CompilerServices;
using AutoFixture;
using Cabazure.Test.Customizations;

namespace Cabazure.Test.Tests;

internal static class TestAssemblyInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        SutFixtureCustomizations.Add(new ProjectWideTestCustomization());
    }
}

/// <summary>A custom value type used only to verify project-wide customization is applied.</summary>
public record ProjectWideValue(string Text);

internal sealed class ProjectWideTestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
        => fixture.Register(() => new ProjectWideValue("project-wide"));
}
