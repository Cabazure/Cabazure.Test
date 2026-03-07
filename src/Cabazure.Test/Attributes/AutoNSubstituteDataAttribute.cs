using System.Reflection;
using Cabazure.Test.Fixture;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Cabazure.Test.Attributes;

/// <summary>
/// An xUnit 3 data attribute that provides theory test method arguments using
/// a <see cref="SutFixture"/>, with automatic NSubstitute substitution for
/// interface and abstract class parameters.
/// </summary>
/// <remarks>
/// <para>
/// Parameters decorated with <see cref="FrozenAttribute"/> are frozen in the fixture
/// before subsequent parameters are resolved, so all later parameters that depend on
/// the same type receive the same instance.
/// </para>
/// <example>
/// <code>
/// [Theory, AutoNSubstituteData]
/// public void Test(IMyService service, MyClass sut)
/// {
///     // service is an NSubstitute substitute
///     // sut is created with service injected
/// }
/// </code>
/// </example>
/// </remarks>
public sealed class AutoNSubstituteDataAttribute : DataAttribute
{
    /// <inheritdoc />
    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
        MethodInfo testMethod,
        DisposalTracker disposalTracker)
    {
        ArgumentNullException.ThrowIfNull(testMethod);

        var fixture = new SutFixture();
        var parameters = testMethod.GetParameters();
        var values = ResolveParameters(fixture, parameters);
        IReadOnlyCollection<ITheoryDataRow> result = [new TheoryDataRow(values)];
        return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(result);
    }

    /// <inheritdoc />
    public override bool SupportsDiscoveryEnumeration() => true;

    private static object[] ResolveParameters(SutFixture fixture, ParameterInfo[] parameters)
    {
        var values = new object[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var isFrozen = parameter.GetCustomAttribute<FrozenAttribute>() is not null;

            var value = CreateValue(fixture, parameter.ParameterType);
            values[i] = value;

            if (isFrozen)
            {
                FreezeValue(fixture, parameter.ParameterType, value);
            }
        }

        return values;
    }

    private static object CreateValue(SutFixture fixture, Type type)
    {
        var createMethod = typeof(SutFixture)
            .GetMethod(nameof(SutFixture.Create))!
            .MakeGenericMethod(type);
        return createMethod.Invoke(fixture, null)!;
    }

    private static void FreezeValue(SutFixture fixture, Type type, object value)
    {
        var freezeMethod = typeof(SutFixture)
            .GetMethods()
            .First(m => m.Name == nameof(SutFixture.Freeze)
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType.IsGenericParameter)
            .MakeGenericMethod(type);
        freezeMethod.Invoke(fixture, [value]);
    }
}
