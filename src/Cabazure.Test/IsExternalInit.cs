#if !NET5_0_OR_GREATER
// Polyfill required for C# 9 `init` accessors and `record` types on netstandard2.1.
// The compiler emits references to this type when compiling init-only properties.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
