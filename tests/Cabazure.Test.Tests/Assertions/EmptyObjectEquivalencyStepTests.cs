using Cabazure.Test;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests.Assertions;

public class EmptyObjectEquivalencyStepTests
{
    [Fact]
    public void BeEquivalentTo_WithAllowingEmptyObjects_PassesForEmptyType()
    {
        var subject = new EmptyDto();
        var expected = new EmptyDto();

        var act = () => subject.Should().BeEquivalentTo(expected, opts => opts.AllowingEmptyObjects());

        act.Should().NotThrow();
    }

    [Fact]
    public void BeEquivalentTo_WithAllowingEmptyObjects_PassesForNonEmptyTypeWithMatchingValues()
    {
        var subject = new DtoWithProperty { Name = "TestName" };
        var expected = new DtoWithProperty { Name = "TestName" };

        var act = () => subject.Should().BeEquivalentTo(expected, opts => opts.AllowingEmptyObjects());

        act.Should().NotThrow();
    }

    [Fact]
    public void BeEquivalentTo_WithAllowingEmptyObjects_ThrowsForNonEmptyTypeWithDifferentValues()
    {
        var subject = new DtoWithProperty { Name = "Name1" };
        var expected = new DtoWithProperty { Name = "Name2" };

        Action act = () => subject.Should().BeEquivalentTo(expected, opts => opts.AllowingEmptyObjects());

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void BeEquivalentTo_WithoutAllowingEmptyObjects_ThrowsForEmptyType()
    {
        var subject = new EmptyDto();
        var expected = new EmptyDto();

        Action act = () => subject.Should().BeEquivalentTo(expected);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void BeEquivalentTo_WithAllowingEmptyObjects_HandlesNullExpectation()
    {
        var subject = new DtoWithProperty { Name = "TestName" };
        DtoWithProperty? expected = null;

        Action act = () => subject.Should().BeEquivalentTo(expected, opts => opts.AllowingEmptyObjects());

        act.Should().Throw<Exception>();
    }

    private sealed class EmptyDto { }

    private sealed class DtoWithProperty
    {
        public string? Name { get; init; }
    }
}
