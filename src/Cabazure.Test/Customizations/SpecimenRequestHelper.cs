using System.Reflection;

namespace Cabazure.Test.Customizations;

/// <summary>
/// Provides helper methods for extracting the requested type from AutoFixture specimen requests.
/// </summary>
/// <remarks>
/// AutoFixture passes requests as different types depending on how a value is being requested:
/// a <see cref="ParameterInfo"/> when resolving a constructor/method parameter, a
/// <see cref="PropertyInfo"/> when resolving a property, a <see cref="FieldInfo"/> when
/// resolving a field, or a bare <see cref="Type"/> when resolved directly. This helper
/// normalizes all four cases into a nullable <see cref="Type"/>.
/// </remarks>
public static class SpecimenRequestHelper
{
    /// <summary>
    /// Extracts the <see cref="Type"/> being requested from an AutoFixture specimen request object.
    /// </summary>
    /// <param name="request">
    /// The request object passed to <see cref="AutoFixture.Kernel.ISpecimenBuilder.Create"/>.
    /// May be a <see cref="ParameterInfo"/>, <see cref="PropertyInfo"/>, <see cref="FieldInfo"/>,
    /// a bare <see cref="Type"/>, or an unrecognized object.
    /// </param>
    /// <returns>
    /// The <see cref="Type"/> being requested, or <see langword="null"/> if the request type
    /// is not recognized (i.e., the builder should return <c>new NoSpecimen()</c>).
    /// </returns>
    public static Type? GetRequestType(object request) => request switch
    {
        ParameterInfo pi => pi.ParameterType,
        PropertyInfo pi => pi.PropertyType,
        FieldInfo fi => fi.FieldType,
        Type t => t,
        _ => null,
    };
}
