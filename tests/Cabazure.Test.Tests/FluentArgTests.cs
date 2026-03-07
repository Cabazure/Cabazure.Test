using Cabazure.Test;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Exceptions;
using Xunit;

namespace Cabazure.Test.Tests;

public class FluentArgTests
{
    public interface ITestService
    {
        void Process(TestRequest request);
        void Log(string message);
        void MultiArg(string name, int value, TestRequest request);
    }

    public class TestRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    [Fact]
    public void Match_PassingAssertion_ReceiveCheckSucceeds()
    {
        var service = Substitute.For<ITestService>();
        service.Process(new TestRequest { Name = "Alice", Amount = 100 });

        var act = () => service.Received(1).Process(
            FluentArg.Match<TestRequest>(r => r.Name.Should().Be("Alice")));

        act.Should().NotThrow();
    }

    [Fact]
    public void Match_FailingAssertion_ReceiveCheckThrowsWithFAMessage()
    {
        var service = Substitute.For<ITestService>();
        service.Process(new TestRequest { Name = "Bob", Amount = 50 });

        var ex = Assert.Throws<ReceivedCallsException>(() =>
            service.Received(1).Process(
                FluentArg.Match<TestRequest>(r => r.Name.Should().Be("Alice"))));

        ex.Message.Should().NotBeNullOrEmpty();
        ex.Message.Should().ContainAny("Bob", "Alice", "Expected");
    }

    [Fact]
    public void Match_NullAssertion_ThrowsArgumentNullException()
    {
        var act = () => FluentArg.Match<TestRequest>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Match_WhenNoCallsReceived_ThrowsReceivedCallsException()
    {
        var service = Substitute.For<ITestService>();

        var act = () => service.Received(1).Process(
            FluentArg.Match<TestRequest>(_ => { }));

        act.Should().Throw<ReceivedCallsException>();
    }
}
