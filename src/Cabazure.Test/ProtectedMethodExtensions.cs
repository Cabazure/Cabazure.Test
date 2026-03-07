using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Cabazure.Test;

/// <summary>
/// Extension methods for invoking protected instance methods on any object via reflection.
/// Supports void, typed-return, and async variants with overload disambiguation and
/// original-exception-stack-trace preservation via <see cref="ExceptionDispatchInfo"/>.
/// </summary>
public static class ProtectedMethodExtensions
{
    /// <summary>
    /// Invokes a protected instance method that has no return value.
    /// </summary>
    /// <param name="target">The object instance on which to invoke the method.</param>
    /// <param name="methodName">The name of the protected method to invoke.</param>
    /// <param name="args">
    /// The arguments to pass to the method. When the method has no parameters, this may be omitted.
    /// </param>
    /// <exception cref="MissingMethodException">
    /// Thrown when no protected method named <paramref name="methodName"/> is found on
    /// <paramref name="target"/>'s type or any base class with a compatible parameter signature.
    /// </exception>
    /// <exception cref="AmbiguousMatchException">
    /// Thrown when multiple protected methods match the name and argument types.
    /// </exception>
    public static void InvokeProtected(this object target, string methodName, params object?[] args)
        => target.InvokeProtected<object>(methodName, args);

    /// <summary>
    /// Invokes a protected instance method and returns its result as <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The expected return type of the method.</typeparam>
    /// <param name="target">The object instance on which to invoke the method.</param>
    /// <param name="methodName">The name of the protected method to invoke.</param>
    /// <param name="args">
    /// The arguments to pass to the method. When the method has no parameters, this may be omitted.
    /// </param>
    /// <returns>The return value of the invoked method, cast to <typeparamref name="TResult"/>.</returns>
    /// <exception cref="MissingMethodException">
    /// Thrown when no protected method named <paramref name="methodName"/> is found on
    /// <paramref name="target"/>'s type or any base class with a compatible parameter signature.
    /// </exception>
    /// <exception cref="AmbiguousMatchException">
    /// Thrown when multiple protected methods match the name and argument types.
    /// </exception>
    public static TResult InvokeProtected<TResult>(this object target, string methodName, params object?[] args)
    {
        args ??= Array.Empty<object?>();
        var method = ResolveMethod(target.GetType(), methodName, args);

        try
        {
            return (TResult)method.Invoke(target, args)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable; satisfies the compiler
        }
    }

    /// <summary>
    /// Invokes a protected instance method that returns <see cref="Task"/> and awaits it.
    /// </summary>
    /// <param name="target">The object instance on which to invoke the method.</param>
    /// <param name="methodName">The name of the protected async method to invoke.</param>
    /// <param name="args">
    /// The arguments to pass to the method. When the method has no parameters, this may be omitted.
    /// </param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    /// <exception cref="MissingMethodException">
    /// Thrown when no protected method named <paramref name="methodName"/> is found on
    /// <paramref name="target"/>'s type or any base class with a compatible parameter signature.
    /// </exception>
    /// <exception cref="AmbiguousMatchException">
    /// Thrown when multiple protected methods match the name and argument types.
    /// </exception>
    public static Task InvokeProtectedAsync(this object target, string methodName, params object?[] args)
        => target.InvokeProtectedAsync<object>(methodName, args);

    /// <summary>
    /// Invokes a protected instance method that returns <see cref="Task{TResult}"/> and awaits it.
    /// </summary>
    /// <typeparam name="TResult">The expected result type of the returned task.</typeparam>
    /// <param name="target">The object instance on which to invoke the method.</param>
    /// <param name="methodName">The name of the protected async method to invoke.</param>
    /// <param name="args">
    /// The arguments to pass to the method. When the method has no parameters, this may be omitted.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation and
    /// yields the method's return value.
    /// </returns>
    /// <exception cref="MissingMethodException">
    /// Thrown when no protected method named <paramref name="methodName"/> is found on
    /// <paramref name="target"/>'s type or any base class with a compatible parameter signature.
    /// </exception>
    /// <exception cref="AmbiguousMatchException">
    /// Thrown when multiple protected methods match the name and argument types.
    /// </exception>
    public static async Task<TResult> InvokeProtectedAsync<TResult>(this object target, string methodName, params object?[] args)
    {
        args ??= Array.Empty<object?>();
        var method = ResolveMethod(target.GetType(), methodName, args);

        try
        {
            var result = method.Invoke(target, args);

            if (result is Task<TResult> typedTask)
            {
                return await typedTask.ConfigureAwait(false);
            }

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                return default!;
            }

            return (TResult)result!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable; satisfies the compiler
        }
    }

    private static MethodInfo ResolveMethod(Type type, string methodName, object?[] args)
    {
        const BindingFlags flags =
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        var candidates = type
            .GetMethods(flags)
            .Where(m => m.Name == methodName && !m.IsPrivate)
            .ToArray();

        // Narrow by parameter count
        var byCount = candidates
            .Where(m => m.GetParameters().Length == args.Length)
            .ToArray();

        if (byCount.Length == 0)
        {
            throw new MissingMethodException(
                $"No protected method '{methodName}' found on type '{type.Name}' " +
                $"matching ({BuildArgTypeList(args)}).");
        }

        if (byCount.Length == 1)
        {
            return byCount[0];
        }

        // Narrow further by type compatibility
        var byType = byCount
            .Where(m => ParametersMatch(m.GetParameters(), args))
            .ToArray();

        return byType.Length switch
        {
            1 => byType[0],
            0 => throw new MissingMethodException(
                $"No protected method '{methodName}' found on type '{type.Name}' " +
                $"matching ({BuildArgTypeList(args)})."),
            _ => throw new AmbiguousMatchException(
                $"Ambiguous match for protected method '{methodName}' on type '{type.Name}'. " +
                $"Candidates: {BuildCandidateList(byType)}."),
        };
    }

    private static bool ParametersMatch(ParameterInfo[] parameters, object?[] args)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            var argType = args[i]?.GetType() ?? typeof(object);
            if (!parameters[i].ParameterType.IsAssignableFrom(argType))
            {
                return false;
            }
        }

        return true;
    }

    private static string BuildArgTypeList(object?[] args)
        => string.Join(", ", args.Select(a => a?.GetType().Name ?? "null"));

    private static string BuildCandidateList(MethodInfo[] methods)
        => string.Join(", ", methods.Select(m =>
            $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})"));
}
