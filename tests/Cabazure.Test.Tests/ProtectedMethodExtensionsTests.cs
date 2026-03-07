using Cabazure.Test;
using FluentAssertions;
using Xunit;

namespace Cabazure.Test.Tests;

public class ProtectedMethodExtensionsTests
{
    private abstract class ProtectedMethodBase
    {
        protected bool voidMethodCalled;

        protected void VoidMethod()
            => voidMethodCalled = true;

        protected int GetValue()
            => 42;

        protected string Combine(string prefix, int number)
            => $"{prefix}-{number}";

        protected void ThrowingMethod()
            => throw new InvalidOperationException("Protected method threw");
    }

    private class ProtectedMethodTarget : ProtectedMethodBase
    {
        public bool WasVoidMethodCalled() => voidMethodCalled;
    }

    private class OverloadedMethodTarget
    {
        protected string Overloaded(string value) => $"string:{value}";
        protected string Overloaded(int value) => $"int:{value}";
    }

    private class AsyncMethodTarget
    {
        protected Task TaskMethod() => Task.CompletedTask;
        protected Task<string> TaskOfStringMethod() => Task.FromResult("async-result");
    }

    private class ThrowingTarget
    {
        protected void ThrowingMethod()
            => throw new InvalidOperationException("Protected method threw");
    }

    [Fact]
    public void InvokeProtected_VoidMethod_ExecutesWithoutReturn()
    {
        var target = new ProtectedMethodTarget();

        target.InvokeProtected("VoidMethod");

        target.WasVoidMethodCalled().Should().BeTrue();
    }

    [Fact]
    public void InvokeProtected_WithReturnValue_ReturnsTypedResult()
    {
        var target = new ProtectedMethodTarget();

        var result = target.InvokeProtected<int>("GetValue");

        result.Should().Be(42);
    }

    [Fact]
    public void InvokeProtected_MethodOnBaseClass_FindsAndInvokes()
    {
        // target is DerivedClass; GetValue() is declared only on ProtectedMethodBase
        var target = new ProtectedMethodTarget();

        var result = target.InvokeProtected<int>("GetValue");

        result.Should().Be(42);
    }

    [Fact]
    public void InvokeProtected_WithZeroArguments_Succeeds()
    {
        var target = new ProtectedMethodTarget();

        target.InvokeProtected("VoidMethod");

        target.WasVoidMethodCalled().Should().BeTrue();
    }

    [Fact]
    public void InvokeProtected_WithMultipleArguments_PassesAllArgs()
    {
        var target = new ProtectedMethodTarget();

        var result = target.InvokeProtected<string>("Combine", "hello", 7);

        result.Should().Be("hello-7");
    }

    [Fact]
    public void InvokeProtected_OverloadedMethod_SelectsCorrectOverload()
    {
        var target = new OverloadedMethodTarget();

        var stringResult = target.InvokeProtected<string>("Overloaded", "test");
        var intResult = target.InvokeProtected<string>("Overloaded", 99);

        stringResult.Should().Be("string:test");
        intResult.Should().Be("int:99");
    }

    [Fact]
    public async Task InvokeProtectedAsync_TaskMethod_AwaitsCompletion()
    {
        var target = new AsyncMethodTarget();

        await target.InvokeProtectedAsync("TaskMethod");
    }

    [Fact]
    public async Task InvokeProtectedAsync_TaskOfTMethod_ReturnsTypedResult()
    {
        var target = new AsyncMethodTarget();

        var result = await target.InvokeProtectedAsync<string>("TaskOfStringMethod");

        result.Should().Be("async-result");
    }

    [Fact]
    public void InvokeProtected_MethodNotFound_ThrowsMissingMethodException()
    {
        var target = new ProtectedMethodTarget();

        var act = () => target.InvokeProtected("NonExistentMethod");

        act.Should().Throw<MissingMethodException>();
    }

    [Fact]
    public void InvokeProtected_MethodThrows_SurfacesOriginalException()
    {
        var target = new ThrowingTarget();

        var act = () => target.InvokeProtected("ThrowingMethod");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Protected method threw");
    }
}
