using AutoFixture;
using AutoFixture.Kernel;
using FrozenAttribute = AutoFixture.Xunit3.FrozenAttribute;
using Cabazure.Test.Attributes;
using Cabazure.Test.Customizations;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Cabazure.Test.Tests.Customizations;

public class TypeCustomizationTests
{
    private sealed class HasIntProperty
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Create_ReturnsDelegateResult_WhenTypeMatchesExactly()
    {
        var fixture = new AutoFixture.Fixture();
        var sut = new TypeCustomization<int>(f => 42);

        sut.Customize(fixture);
        var result = fixture.Create<int>();

        result.Should().Be(42);
    }

    [Fact]
    public void Create_ReturnsDelegateResult_ForReferenceType()
    {
        var fixture = new AutoFixture.Fixture();
        const string expectedValue = "custom-string";
        var sut = new TypeCustomization<string>(f => expectedValue);

        sut.Customize(fixture);
        var result = fixture.Create<string>();

        result.Should().Be(expectedValue);
    }

    [Fact]
    public void Create_ReturnsNoSpecimen_ForNonMatchingType()
    {
        var fixture = new AutoFixture.Fixture();
        var sut = new TypeCustomization<int>(f => 42);
        sut.Customize(fixture);

        var result = fixture.Create<string>();

        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("42");
    }

    [Fact]
    public void Customize_ThrowsArgumentNullException_WhenFixtureIsNull()
    {
        var sut = new TypeCustomization<int>(f => 42);

        var act = () => sut.Customize(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fixture");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenFactoryIsNull()
    {
        var act = () => new TypeCustomization<int>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("factory");
    }

    [Fact]
    public void Factory_ReceivesIFixture_WithWorkingCreate()
    {
        var fixture = new AutoFixture.Fixture();
        string? capturedString = null;

        var sut = new TypeCustomization<int>(f =>
        {
            capturedString = f.Create<string>();
            return 99;
        });

        sut.Customize(fixture);
        var result = fixture.Create<int>();

        result.Should().Be(99);
        capturedString.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TypeCustomization_CanBeWrappedInCustomClass()
    {
        var fixture = new AutoFixture.Fixture();
        var expectedDate = new DateTime(2025, 12, 25);
        var sut = new FixedDateTimeCustomization(expectedDate);

        sut.Customize(fixture);
        var result = fixture.Create<DateTime>();

        result.Should().Be(expectedDate);
    }

    [Fact]
    public void Add_WithFactory_CreatesAndAddsTypeCustomization()
    {
        var fixture = new AutoFixture.Fixture();
        var customizations = new FixtureCustomizationCollection();
        
        customizations.Add<int>(f => 99);

        foreach (var customization in customizations)
        {
            customization.Customize(fixture);
        }

        var result = fixture.Create<int>();
        result.Should().Be(99);
    }

    [Fact]
    public void Add_WithFactory_ThrowsArgumentNullException_WhenFactoryIsNull()
    {
        var customizations = new FixtureCustomizationCollection();

        var act = () => customizations.Add<int>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("factory");
    }

    [Fact]
    public void Add_WithSpecimenBuilder_RegistersBuilder()
    {
        var fixture = new AutoFixture.Fixture();
        var customizations = new FixtureCustomizationCollection();
        var mockBuilder = Substitute.For<ISpecimenBuilder>();
        const int expectedValue = 777;

        mockBuilder.Create(Arg.Any<object>(), Arg.Any<ISpecimenContext>())
            .Returns(callInfo =>
            {
                var request = callInfo[0];
                return request is Type t && t == typeof(int)
                    ? expectedValue
                    : new NoSpecimen();
            });

        customizations.Add(mockBuilder);

        foreach (var customization in customizations)
        {
            customization.Customize(fixture);
        }

        var result = fixture.Create<int>();
        result.Should().Be(expectedValue);
        mockBuilder.Received().Create(Arg.Any<object>(), Arg.Any<ISpecimenContext>());
    }

    [Fact]
    public void Add_WithSpecimenBuilder_ThrowsArgumentNullException_WhenBuilderIsNull()
    {
        var customizations = new FixtureCustomizationCollection();

        var act = () => customizations.Add((ISpecimenBuilder)null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    [Fact]
    public void TypeCustomization_CanUseIFixture_Build_T()
    {
        var fixture = new AutoFixture.Fixture();
        const int customValue = 12345;

        var sut = new TypeCustomization<HasIntProperty>(f =>
            f.Build<HasIntProperty>()
                .With(x => x.Value, customValue)
                .Create());

        sut.Customize(fixture);
        var result = fixture.Create<HasIntProperty>();

        result.Value.Should().Be(customValue);
    }

    [Theory, AutoNSubstituteData]
    public void Create_UsesFactory_ForConstructorParameter(
        [Frozen] IFixture baseFixture)
    {
        const int customValue = 555;
        var sut = new TypeCustomization<int>(f => customValue);

        sut.Customize(baseFixture);
        var result = baseFixture.Create<ClassWithIntConstructor>();

        result.Value.Should().Be(customValue);
    }

    [Theory, AutoNSubstituteData]
    public void Create_UsesFactory_MultipleTimesForMultipleRequests(
        [Frozen] IFixture baseFixture)
    {
        var callCount = 0;
        var sut = new TypeCustomization<int>(f => ++callCount);

        sut.Customize(baseFixture);
        var first = baseFixture.Create<int>();
        var second = baseFixture.Create<int>();

        first.Should().Be(1);
        second.Should().Be(2);
    }

    [Fact]
    public void Create_DoesNotInterfere_WithOtherCustomizations()
    {
        var fixture = FixtureFactory.Create();
        var sut = new TypeCustomization<int>(f => 42);

        sut.Customize(fixture);
        var intResult = fixture.Create<int>();
        var stringResult = fixture.Create<string>();
        var guidResult = fixture.Create<Guid>();

        intResult.Should().Be(42);
        stringResult.Should().NotBeNullOrEmpty();
        guidResult.Should().NotBeEmpty();
    }

    private sealed class FixedDateTimeCustomization : ICustomization
    {
        private readonly DateTime value;

        public FixedDateTimeCustomization(DateTime value)
        {
            this.value = value;
        }

        public void Customize(IFixture fixture)
        {
            var typeCustomization = new TypeCustomization<DateTime>(f => value);
            typeCustomization.Customize(fixture);
        }
    }

    private sealed class ClassWithIntConstructor
    {
        public int Value { get; }

        public ClassWithIntConstructor(int value)
        {
            Value = value;
        }
    }
}
