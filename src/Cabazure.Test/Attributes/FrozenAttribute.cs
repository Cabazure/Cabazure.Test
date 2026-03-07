namespace Cabazure.Test.Attributes;

/// <summary>
/// When applied to a parameter of an <see cref="AutoNSubstituteDataAttribute"/>-decorated
/// theory method, causes the parameter value to be frozen in the <see cref="Fixture.SutFixture"/>
/// so that all subsequent parameters of the same type receive the same instance.
/// </summary>
/// <remarks>
/// Freeze parameters from left to right. A frozen parameter is registered before
/// subsequent parameters are resolved, ensuring consistent injection.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FrozenAttribute : Attribute
{
}
