using NSubstitute;
using NSubstitute.Core;

namespace Cabazure.Test;

/// <summary>
/// Extension methods for inspecting arguments of received NSubstitute calls,
/// consistent with NSubstitute's own extension-method-on-object style.
/// </summary>
public static class ReceivedCallExtensions
{
    /// <summary>
    /// Returns the argument of type <typeparamref name="T"/> from the most recently received call.
    /// </summary>
    /// <typeparam name="T">The type of the argument to retrieve.</typeparam>
    /// <param name="substitute">The NSubstitute substitute to inspect.</param>
    /// <returns>The first argument of type <typeparamref name="T"/> from the last received call.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no received calls exist, or when no argument of type <typeparamref name="T"/>
    /// is found in the last call.
    /// </exception>
    public static T ReceivedArg<T>(this object substitute)
    {
        var lastCall = substitute.ReceivedCalls().LastOrDefault()
            ?? throw new InvalidOperationException(
                $"No received calls found on substitute of type {substitute.GetType().Name}.");

        foreach (var arg in lastCall.GetArguments())
        {
            if (arg is T typed)
            {
                return typed;
            }
        }

        throw new InvalidOperationException(
            $"No argument of type {typeof(T).FullName} found in the last received call to {lastCall.GetMethodInfo().Name}.");
    }

    /// <summary>
    /// Returns all arguments of type <typeparamref name="T"/> across all received calls, in chronological order.
    /// </summary>
    /// <typeparam name="T">The type of the arguments to retrieve.</typeparam>
    /// <param name="substitute">The NSubstitute substitute to inspect.</param>
    /// <returns>
    /// An enumerable of all arguments of type <typeparamref name="T"/> found across all received calls.
    /// Returns an empty enumerable if no calls were received or none of their arguments match.
    /// </returns>
    public static IEnumerable<T> ReceivedArgs<T>(this object substitute)
    {
        foreach (var call in substitute.ReceivedCalls())
        {
            foreach (var arg in call.GetArguments())
            {
                if (arg is T typed)
                {
                    yield return typed;
                }
            }
        }
    }
}
