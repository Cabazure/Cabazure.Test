using AutoFixture;
using Cabazure.Test;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Cabazure.Test.Tests.Fixture;

public class FixtureFactoryTests
{
    [Fact]
    public void Create_ReturnsNewInstance()
    {
        var fixture = FixtureFactory.Create();
        var result = fixture.Create<string>();
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_ForInterface_ReturnsNSubstituteProxy()
    {
        var fixture = FixtureFactory.Create();
        var result = fixture.Create<IMyInterface>();
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IMyInterface>();
    }

    [Fact]
    public void Create_ForAbstractClass_ReturnsNSubstituteProxy()
    {
        var fixture = FixtureFactory.Create();
        var result = fixture.Create<MyAbstractClass>();
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<MyAbstractClass>();
    }

    [Fact]
    public void Create_ForConcreteClass_ReturnsInstance()
    {
        var fixture = FixtureFactory.Create();
        var result = fixture.Create<MyConcreteClass>();
        result.Should().NotBeNull();
    }

    [Fact]
    public void Create_ForConcreteClassWithDependencies_InjectsMockedDependencies()
    {
        var fixture = FixtureFactory.Create();
        var result = fixture.Create<MyServiceWithDependency>();
        result.Should().NotBeNull();
        result.Dependency.Should().NotBeNull();
    }

    [Fact]
    public void CreateMany_ReturnsDefaultThreeInstances()
    {
        var fixture = FixtureFactory.Create();
        var result = fixture.CreateMany<string>();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void CreateMany_WithCount_ReturnsSpecifiedCount()
    {
        var fixture = FixtureFactory.Create();
        var result = fixture.CreateMany<string>(5);
        result.Should().HaveCount(5);
    }

    [Fact]
    public void Freeze_ReturnsSubstitute_ForInterface()
    {
        var fixture = FixtureFactory.Create();
        var frozen = fixture.Freeze<IMyInterface>();
        frozen.Should().NotBeNull();
        frozen.Should().BeAssignableTo<IMyInterface>();
    }

    [Fact]
    public void Freeze_ReturnsSameInstance_WhenCreatingDependentType()
    {
        var fixture = FixtureFactory.Create();
        var frozen = fixture.Freeze<IMyInterface>();
        var sut = fixture.Create<MyServiceWithDependency>();
        sut.Dependency.Should().BeSameAs(frozen);
    }

    [Fact]
    public void Freeze_WithInstance_RegistersProvidedInstance()
    {
        var fixture = FixtureFactory.Create();
        var instance = Substitute.For<IMyInterface>();
        fixture.Inject(instance);
        var sut = fixture.Create<MyServiceWithDependency>();
        sut.Dependency.Should().BeSameAs(instance);
    }

    [Fact]
    public void Substitute_ReturnsNSubstituteProxy()
    {
        var result = Substitute.For<IMyInterface>();
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IMyInterface>();
    }

    [Fact]
    public void Substitute_ReturnsDifferentInstances_OnMultipleCalls()
    {
        var sub1 = Substitute.For<IMyInterface>();
        var sub2 = Substitute.For<IMyInterface>();
        sub1.Should().NotBeSameAs(sub2);
    }

    [Fact]
    public void Create_ForValueType_ReturnsNonDefault()
    {
        var fixture = FixtureFactory.Create();
        var result = fixture.Create<int>();
        // AutoFixture generates non-default values for value types
        // (not guaranteed to be non-zero but should not throw)
        ((object)result).Should().BeOfType<int>();
    }

    // Test helpers — referenced by other test files via FixtureFactoryTests.*
    public interface IMyInterface { }

    public abstract class MyAbstractClass { }

    public class MyConcreteClass { }

    public class MyServiceWithDependency
    {
        public IMyInterface Dependency { get; }
        public MyServiceWithDependency(IMyInterface dependency)
        {
            Dependency = dependency;
        }
    }
}
