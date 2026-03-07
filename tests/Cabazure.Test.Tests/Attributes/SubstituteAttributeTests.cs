using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Cabazure.Test.Attributes;
using Cabazure.Test.Tests.Fixture;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace Cabazure.Test.Tests.Attributes;

public class SubstituteAttributeTests
{
    public class ConcreteService
    {
        public virtual string GetValue() => "real";
    }

    [Theory, AutoNSubstituteData]
    public void Theory_SubstituteAttribute_OnInterface_CreatesNSubstituteProxy(
        [Substitute] FixtureFactoryTests.IMyInterface service)
    {
        SubstitutionContext.Current.GetCallRouterFor(service).Should().NotBeNull();
    }

    [Theory, AutoNSubstituteData]
    public void Theory_SubstituteAttribute_OnConcreteClass_CreatesNSubstituteProxy(
        [Substitute] ConcreteService service)
    {
        service.GetValue().Returns("mocked");
        service.GetValue().Should().Be("mocked");
    }

    [Theory, AutoNSubstituteData]
    public void Theory_SubstituteAttribute_OnConcreteClass_IsNotRealInstance(
        [Substitute] ConcreteService service)
    {
        service.GetValue().Should().NotBe("real");
    }

    [Theory, AutoNSubstituteData]
    public void Theory_SubstituteAttribute_WithFrozen_FreezesSameSubstituteInstance(
        [Frozen, Substitute] FixtureFactoryTests.IMyInterface service,
        FixtureFactoryTests.MyServiceWithDependency sut)
    {
        sut.Dependency.Should().BeSameAs(service);
    }

    [Theory, AutoNSubstituteData]
    public void Theory_SubstituteAttribute_WithoutFreeze_DoesNotAffectOtherParameters(
        [Substitute] FixtureFactoryTests.IMyInterface service,
        [Substitute] FixtureFactoryTests.IMyInterface service2)
    {
        service.Should().NotBeSameAs(service2);
    }
}
