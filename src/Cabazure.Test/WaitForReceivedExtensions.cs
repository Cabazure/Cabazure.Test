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
        if (substitute is null) throw new ArgumentNullException(nameof(substitute));
        if (callSpec is null) throw new ArgumentNullException(nameof(callSpec));

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
        if (substitute is null) throw new ArgumentNullException(nameof(substitute));
        if (callSpec is null) throw new ArgumentNullException(nameof(callSpec));

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

    private static async Task WaitForCallAsync(object substitute, ICallSpecification spec, TimeSpan? timeout)
    {
        var context = SubstitutionContext.Current;
        var router = context.GetCallRouterFor(substitute);

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Register handler for future calls (event-driven — works when no configured return)
        router.RegisterCustomCallHandlerFactory(state => new SignalingCallHandler(spec, tcs));

        // Pre-check already-received calls (race-free because handler is already registered)
        if (router.ReceivedCalls().Any(c => spec.IsSatisfiedBy(c)))
        {
            tcs.TrySetResult(null);
        }

        var effectiveTimeout = timeout ?? DefaultTimeout;
        var ct = TestContext.Current.CancellationToken;

        // Polling fallback: NSubstitute stops the route chain at ReturnConfiguredResultHandler (step 6)
        // before reaching ReturnFromCustomHandlers (step 9) where SignalingCallHandler lives.
        // RecordCallHandler (step 2) always runs, so ReceivedCalls() always has the call.
        // This polling loop catches the case where the event-driven handler is never reached.
        _ = PollUntilSignaledAsync(router, spec, tcs, ct);

#if NET6_0_OR_GREATER
        await tcs.Task.WaitAsync(effectiveTimeout, ct).ConfigureAwait(false);
#else
        var delayTask = Task.Delay(effectiveTimeout, ct);
        var completed = await Task.WhenAny(tcs.Task, delayTask).ConfigureAwait(false);
        if (completed == delayTask)
        {
            ct.ThrowIfCancellationRequested();
            throw new TimeoutException($"The operation did not complete within {effectiveTimeout}.");
        }
        await tcs.Task.ConfigureAwait(false);
#endif
    }

    /// <summary>
    /// Polls for calls matching the specification in the router's received calls list,
    /// signaling the TaskCompletionSource when a match is found. Provides fallback detection
    /// for scenarios where the event-driven SignalingCallHandler is bypassed by NSubstitute's
    /// route chain (e.g., when a substitute has a configured return value).
    /// </summary>
    private static async Task PollUntilSignaledAsync(
        ICallRouter router,
        ICallSpecification spec,
        TaskCompletionSource<object?> tcs,
        CancellationToken ct)
    {
        try
        {
            while (!tcs.Task.IsCompleted)
            {
                await Task.Delay(50, ct);
                if (tcs.Task.IsCompleted)
                    return;
                if (router.ReceivedCalls().Any(c => spec.IsSatisfiedBy(c)))
                {
                    tcs.TrySetResult(null);
                    return;
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}

/// <summary>
/// NSubstitute call handler that signals a <see cref="TaskCompletionSource{T}"/> when a call
/// matching the specified call specification is received. Used internally by
/// <see cref="WaitForReceivedExtensions"/> to enable asynchronous call waiting.
/// </summary>
internal sealed class SignalingCallHandler : ICallHandler
{
    private readonly ICallSpecification spec;
    private readonly TaskCompletionSource<object?> tcs;

    public SignalingCallHandler(ICallSpecification spec, TaskCompletionSource<object?> tcs)
    {
        this.spec = spec;
        this.tcs = tcs;
    }

    /// <inheritdoc />
    public RouteAction Handle(ICall call)
    {
        if (spec.IsSatisfiedBy(call))
        {
            tcs.TrySetResult(null);
        }
        return RouteAction.Continue();
    }
}
