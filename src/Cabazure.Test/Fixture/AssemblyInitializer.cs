using System.Runtime.CompilerServices;

namespace Cabazure.Test.Fixture;

/// <summary>
/// Performs assembly-level initialization for Cabazure.Test using the .NET
/// module initializer pattern, which xUnit 3 supports natively.
/// </summary>
internal static class AssemblyInitializer
{
    /// <summary>
    /// Runs when the Cabazure.Test assembly is first loaded.
    /// Use this for any one-time global configuration required by the library.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Reserved for future global xUnit 3 configuration.
        // Using [ModuleInitializer] (xUnit 3 recommended pattern) instead of
        // static constructors avoids ordering issues in test runners.
    }
}
