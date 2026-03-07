using Cabazure.Test;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Cabazure.Test.Tests;

public class ReceivedCallExtensionsTests
{
    public interface IReceivedTestService
    {
        void Process(TestRequest request);
        void Log(string message);
        void MultiArg(string name, int value);
    }

    public class TestRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    [Fact]
    public void ReceivedArg_SingleCall_ReturnsArgFromLastCall()
    {
        var service = Substitute.For<IReceivedTestService>();
        service.Process(new TestRequest { Name = "Alice", Amount = 100 });

        var result = service.ReceivedArg<TestRequest>();

        result.Name.Should().Be("Alice");
        result.Amount.Should().Be(100);
    }

    [Fact]
    public void ReceivedArg_MultipleCalls_ReturnsArgFromLastCall()
    {
        var service = Substitute.For<IReceivedTestService>();
        service.Process(new TestRequest { Name = "First", Amount = 1 });
        service.Process(new TestRequest { Name = "Second", Amount = 2 });

        var result = service.ReceivedArg<TestRequest>();

        result.Name.Should().Be("Second");
        result.Amount.Should().Be(2);
    }

    [Fact]
    public void ReceivedArg_NoCalls_ThrowsInvalidOperationException()
    {
        var service = Substitute.For<IReceivedTestService>();

        var act = () => service.ReceivedArg<TestRequest>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReceivedArg_ArgNotFoundInLastCall_ThrowsInvalidOperationException()
    {
        var service = Substitute.For<IReceivedTestService>();
        service.Log("hello");

        var act = () => service.ReceivedArg<TestRequest>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReceivedArgs_MultipleCalls_ReturnsAllArgsInOrder()
    {
        var service = Substitute.For<IReceivedTestService>();
        service.Log("first");
        service.Log("second");
        service.Log("third");

        var result = service.ReceivedArgs<string>();

        result.Should().Equal("first", "second", "third");
    }

    [Fact]
    public void ReceivedArgs_NoCalls_ReturnsEmptyEnumerable()
    {
        var service = Substitute.For<IReceivedTestService>();

        var result = service.ReceivedArgs<string>();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ReceivedArgs_MixedArgTypes_ReturnsOnlyMatchingType()
    {
        var service = Substitute.For<IReceivedTestService>();
        service.MultiArg("hello", 42);

        service.ReceivedArgs<string>().Should().Equal("hello");
        service.ReceivedArgs<int>().Should().Equal(42);
    }

    [Fact]
    public void ReceivedArg_CombinedWithFluentAssertions_WorksEndToEnd()
    {
        var service = Substitute.For<IReceivedTestService>();
        service.Process(new TestRequest { Name = "Alice", Amount = 100 });

        service.Received(1).Process(Arg.Any<TestRequest>());

        service.ReceivedArg<TestRequest>().Should().BeEquivalentTo(
            new TestRequest { Name = "Alice", Amount = 100 });
    }
}
