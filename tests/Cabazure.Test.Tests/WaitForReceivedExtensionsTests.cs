using Cabazure.Test;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Cabazure.Test.Tests;

public class WaitForReceivedExtensionsTests
{
    public interface ITestService
    {
        void Process(string value);
        Task ProcessAsync(string value);
        Task<string> FetchAsync(string input);
    }

    [Fact]
    public async Task WaitForReceived_AlreadyReceivedExactMatch_ReturnsImmediately()
    {
        var service = Substitute.For<ITestService>();
        service.Process("test");

        var act = async () => await service.WaitForReceived(
            s => s.Process("test"),
            timeout: TimeSpan.FromMilliseconds(100));

        await act.Should().CompleteWithinAsync(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public async Task WaitForReceivedWithAnyArgs_AlreadyReceived_ReturnsImmediately()
    {
        var service = Substitute.For<ITestService>();
        service.Process("original");

        var act = async () => await service.WaitForReceivedWithAnyArgs(
            s => s.Process("different"),
            timeout: TimeSpan.FromMilliseconds(100));

        await act.Should().CompleteWithinAsync(TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public async Task WaitForReceived_CallArrivesAfterAwaiting_CompletesTask()
    {
        var service = Substitute.For<ITestService>();

        var waitTask = service.WaitForReceived(
            s => s.Process("delayed"),
            timeout: TimeSpan.FromSeconds(5));

        await Task.Delay(50);
        _ = Task.Run(() => service.Process("delayed"));

        var act = async () => await waitTask;
        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForReceived_NonMatchingArgument_ThrowsTimeoutException()
    {
        var service = Substitute.For<ITestService>();
        service.Process("wrong");

        var act = async () => await service.WaitForReceived(
            s => s.Process("expected"),
            timeout: TimeSpan.FromMilliseconds(100));

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task WaitForReceivedWithAnyArgs_DifferentArgumentValues_Completes()
    {
        var service = Substitute.For<ITestService>();

        var waitTask = service.WaitForReceivedWithAnyArgs(
            s => s.Process("any"),
            timeout: TimeSpan.FromSeconds(5));

        await Task.Delay(50);
        _ = Task.Run(() => service.Process("different"));

        var act = async () => await waitTask;
        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForReceived_TimeoutExpiry_ThrowsTimeoutException()
    {
        var service = Substitute.For<ITestService>();

        var act = async () => await service.WaitForReceived(
            s => s.Process("never-called"),
            timeout: TimeSpan.FromMilliseconds(100));

        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task WaitForReceived_MultipleConcurrentWaiters_AllComplete()
    {
        var service = Substitute.For<ITestService>();

        var wait1 = service.WaitForReceived(
            s => s.Process("shared"),
            timeout: TimeSpan.FromSeconds(5));

        var wait2 = service.WaitForReceived(
            s => s.Process("shared"),
            timeout: TimeSpan.FromSeconds(5));

        await Task.Delay(50);
        _ = Task.Run(() => service.Process("shared"));

        var act = async () => await Task.WhenAll(wait1, wait2);
        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForReceived_CustomTimeoutParameter_OverridesDefaultTimeout()
    {
        var originalDefault = WaitForReceivedExtensions.DefaultTimeout;
        try
        {
            WaitForReceivedExtensions.DefaultTimeout = TimeSpan.FromSeconds(30);

            var service = Substitute.For<ITestService>();
            var startTime = DateTime.UtcNow;

            var act = async () => await service.WaitForReceived(
                s => s.Process("never"),
                timeout: TimeSpan.FromMilliseconds(100));

            await act.Should().ThrowAsync<TimeoutException>();

            var elapsed = DateTime.UtcNow - startTime;
            elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
        }
        finally
        {
            WaitForReceivedExtensions.DefaultTimeout = originalDefault;
        }
    }

    [Fact]
    public async Task WaitForReceivedWithAnyArgs_WhenSubstituteHasConfiguredReturn_StillDetectsCall()
    {
        var service = Substitute.For<ITestService>();
        service.FetchAsync(Arg.Any<string>()).Returns("result");

        var waitTask = service.WaitForReceivedWithAnyArgs(
            s => s.FetchAsync(default!),
            timeout: TimeSpan.FromSeconds(5));

        await Task.Delay(50);
        _ = Task.Run(async () => await service.FetchAsync("input"));

        var act = async () => await waitTask;
        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WaitForReceived_WhenSubstituteHasConfiguredReturn_StillDetectsCall()
    {
        var service = Substitute.For<ITestService>();
        service.FetchAsync("exact-input").Returns("result");

        var waitTask = service.WaitForReceived(
            s => s.FetchAsync("exact-input"),
            timeout: TimeSpan.FromSeconds(5));

        await Task.Delay(50);
        _ = Task.Run(async () => await service.FetchAsync("exact-input"));

        var act = async () => await waitTask;
        await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(1));
    }
}
