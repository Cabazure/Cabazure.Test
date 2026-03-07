using NSubstitute.Core;
using Xunit;

namespace Cabazure.Test;

/// <summary>
/// Extension methods for asynchronously waiting for NSubstitute calls to occur,
/// useful for verifying behavior in concurrent or asynchronous code under test.
/// </summary>
public static class WaitForReceivedExtensions
{
    /// <summary>
    /// The default timeout used when no explicit timeout is provided to
    /// <see cref="WaitForReceived{T}"/> or <see cref="WaitForReceivedWithAnyArgs{T}"/>.
    /// Users can set this in a <c>[ModuleInitializer]</c> to configure test-suite-wide behavior.
    /// </summary>
    public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Asynchronously waits for a call matching the exact arguments specified in the call specification.
    /// </summary>
    /// <typeparam name="T">The type of the substitute.</typeparam>
    /// <param name="substitute">The NSubstitute substitute to monitor for calls.</param>
    /// <param name="callSpec">
    /// A call specification expression (e.g., <c>x => x.Process(expectedRequest)</c>)
    /// that captures the method and exact argument values to match.
    /// </param>
    /// <param name="timeout">
    /// The maximum time to wait for the call. When <c>null</c>, <see cref="DefaultTimeout"/> is used.
    /// Pass <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.
    /// </param>
    /// <returns>
    /// A task that completes when a matching call is received or when already present.
    /// </returns>
    /// <exception cref="TimeoutException">Thrown when the timeout expires before a matching call is received.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the <see cref="TestContext.Current"/> cancellation token is canceled.
    /// </exception>
    public static Task WaitForReceived<T>(this T substitute, Action<T> callSpec, TimeSpan? timeout = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(substitute);
        ArgumentNullException.ThrowIfNull(callSpec);

        var capturedSpec = CaptureCallSpec(substitute, callSpec, withAnyArgs: false);
        return WaitForCallAsync(substitute, capturedSpec, timeout);
    }

    /// <summary>
    /// Asynchronously waits for any call to the specified method, regardless of argument values.
    /// </summary>
    /// <typeparam name="T">The type of the substitute.</typeparam>
    /// <param name="substitute">The NSubstitute substitute to monitor for calls.</param>
    /// <param name="callSpec">
    /// A call specification expression (e.g., <c>x => x.Process(default!)</c>)
    /// that captures the method to match. Argument values are ignored.
    /// </param>
    /// <param name="timeout">
    /// The maximum time to wait for the call. When <c>null</c>, <see cref="DefaultTimeout"/> is used.
    /// Pass <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.
    /// </param>
    /// <returns>
    /// A task that completes when any call to the method is received or when already present.
    /// </returns>
    /// <exception cref="TimeoutException">Thrown when the timeout expires before a matching call is received.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the <see cref="TestContext.Current"/> cancellation token is canceled.
    /// </exception>
    public static Task WaitForReceivedWithAnyArgs<T>(this T substitute, Action<T> callSpec, TimeSpan? timeout = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(substitute);
        ArgumentNullException.ThrowIfNull(callSpec);

        var capturedSpec = CaptureCallSpec(substitute, callSpec, withAnyArgs: true);
        return WaitForCallAsync(substitute, capturedSpec, timeout);
    }

    private static ICallSpecification CaptureCallSpec<T>(T substitute, Action<T> callSpec, bool withAnyArgs)
        where T : class
    {
        var context = SubstitutionContext.Current;
        var router = context.GetCallRouterFor(substitute);

        // Set up route to capture the call specification
        context.ThreadContext.SetNextRoute(router, state => context.RouteFactory.RecordCallSpecification(state));

        // Trigger the call specification
        callSpec(substitute);

        // Extract the pending specification
        var pendingInfo = context.ThreadContext.PendingSpecification.UseCallSpecInfo();
        if (pendingInfo == null)
        {
            throw new InvalidOperationException("Failed to capture call specification. The call expression may not have invoked a substitute method.");
        }

        // Handle the pending specification to get ICallSpecification
        var capturedSpec = pendingInfo.Handle(
            spec => spec,
            call => context.CallSpecificationFactory.CreateFrom(call, MatchArgs.AsSpecifiedInCall));

        // If withAnyArgs, create a copy that matches any arguments
        if (withAnyArgs)
        {
            capturedSpec = capturedSpec.CreateCopyThatMatchesAnyArguments();
        }

        return capturedSpec;
    }

    private static Task WaitForCallAsync(object substitute, ICallSpecification spec, TimeSpan? timeout)
    {
        var context = SubstitutionContext.Current;
        var router = context.GetCallRouterFor(substitute);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Register handler for future calls
        router.RegisterCustomCallHandlerFactory(state => new SignalingCallHandler(spec, tcs));

        // Pre-check already-received calls (race-free because handler is already registered)
        if (router.ReceivedCalls().Any(c => spec.IsSatisfiedBy(c)))
        {
            tcs.TrySetResult();
        }

        var effectiveTimeout = timeout ?? DefaultTimeout;
        var ct = TestContext.Current.CancellationToken;

        return tcs.Task.WaitAsync(effectiveTimeout, ct);
    }
}

/// <summary>
/// NSubstitute call handler that signals a <see cref="TaskCompletionSource"/> when a call
/// matching the specified call specification is received. Used internally by
/// <see cref="WaitForReceivedExtensions"/> to enable asynchronous call waiting.
/// </summary>
internal sealed class SignalingCallHandler : ICallHandler
{
    private readonly ICallSpecification spec;
    private readonly TaskCompletionSource tcs;

    public SignalingCallHandler(ICallSpecification spec, TaskCompletionSource tcs)
    {
        this.spec = spec;
        this.tcs = tcs;
    }

    /// <inheritdoc />
    public RouteAction Handle(ICall call)
    {
        if (spec.IsSatisfiedBy(call))
        {
            tcs.TrySetResult();
        }
        return RouteAction.Continue();
    }
}
