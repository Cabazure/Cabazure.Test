namespace Cabazure.Test.Attributes;

/// <summary>
/// When applied to a parameter of a <see cref="AutoNSubstituteDataAttribute"/>-family
/// attribute, causes the parameter value to be frozen in the <see cref="FixtureFactory"/>-created
/// <see cref="AutoFixture.IFixture"/> so that all subsequent parameters of the same type receive
/// the same instance. Works for both fixture-generated and explicitly provided values
/// (inline, class, or member data).
/// </summary>
/// <remarks>
/// Freeze parameters from left to right. A frozen parameter is registered in the fixture before
/// subsequent parameters are resolved, ensuring consistent injection into auto-generated types.
/// Value types are not frozen.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FrozenAttribute : Attribute
{
}
