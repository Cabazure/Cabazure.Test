#if NET6_0_OR_GREATER
using AutoFixture;
using AutoFixture.Kernel;

namespace Cabazure.Test.Customizations;

/// <summary>
/// An AutoFixture customization that enables the fixture to reliably create instances
/// of <see cref="DateOnly"/> and <see cref="TimeOnly"/> with meaningfully random values.
/// </summary>
/// <remarks>
/// AutoFixture cannot reliably create <see cref="DateOnly"/> by default because it may
/// generate invalid year/month/day combinations via the <c>(int, int, int)</c> constructor.
/// <see cref="TimeOnly"/> is technically constructible but AutoFixture generates
/// near-zero tick values. Both types are derived here from a randomly generated
/// <see cref="DateTime"/>, ensuring valid and well-distributed results.
/// </remarks>
public sealed class DateOnlyTimeOnlyCustomization : ICustomization
{
    /// <inheritdoc />
    public void Customize(IFixture fixture)
    {
        if (fixture is null) throw new ArgumentNullException(nameof(fixture));
        fixture.Customizations.Add(new DateTimeOnlyBuilder());
    }

    private sealed class DateTimeOnlyBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            var type = SpecimenRequestHelper.GetRequestType(request);

            if (type == typeof(DateOnly))
            {
                var dateTime = (DateTime)context.Resolve(typeof(DateTime));
                return DateOnly.FromDateTime(dateTime);
            }

            if (type == typeof(TimeOnly))
            {
                var dateTime = (DateTime)context.Resolve(typeof(DateTime));
                return TimeOnly.FromDateTime(dateTime);
            }

            return new NoSpecimen();
        }
    }
}
#endif
