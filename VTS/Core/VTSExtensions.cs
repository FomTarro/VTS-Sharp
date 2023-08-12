namespace VTS.Core;

internal static class VTSExtensions
{
    internal static VTSException ToException(this VTSErrorData errorData)
    {
        return new VTSException(errorData);
    }

    internal static Task<TSuccess> Async<TSuccess, TError>(this Action<Action<TSuccess>, Action<TError>> action)
        where TError : VTSErrorData
    {
        var tcs = new TaskCompletionSource<TSuccess>();

        action(
            modelData => tcs.SetResult(modelData),
            errorData => tcs.SetException(errorData.ToException())
        );

        return tcs.Task;
    }

    internal static Task<TSuccess> Async<T1, TSuccess, TError>(this Action<T1, Action<TSuccess>, Action<TError>> action,
        T1 argument1) where TError : VTSErrorData
    {
        var tcs = new TaskCompletionSource<TSuccess>();

        action(
            argument1,
            modelData => tcs.SetResult(modelData),
            errorData => tcs.SetException(errorData.ToException())
        );

        return tcs.Task;
    }

    internal static Task<TSuccess> Async<T1, T2, TSuccess, TError>(
        this Action<T1, T2, Action<TSuccess>, Action<TError>> action, T1 argument1, T2 argument2)
        where TError : VTSErrorData
    {
        var tcs = new TaskCompletionSource<TSuccess>();

        action(
            argument1,
            argument2,
            modelData => tcs.SetResult(modelData),
            errorData => tcs.SetException(errorData.ToException())
        );

        return tcs.Task;
    }

    internal static Task<TSuccess> Async<T1, T2, T3, TSuccess, TError>(
        this Action<T1, T2, T3, Action<TSuccess>, Action<TError>> action, T1 argument1, T2 argument2, T3 argument3)
        where TError : VTSErrorData
    {
        var tcs = new TaskCompletionSource<TSuccess>();

        action(
            argument1,
            argument2,
            argument3,
            modelData => tcs.SetResult(modelData),
            errorData => tcs.SetException(errorData.ToException())
        );

        return tcs.Task;
    }

    internal static Task<TSuccess> Async<T1, T2, T3, T4, TSuccess, TError>(
        this Action<T1, T2, T3, T4, Action<TSuccess>, Action<TError>> action, T1 argument1, T2 argument2, T3 argument3,
        T4 argument4) where TError : VTSErrorData
    {
        var tcs = new TaskCompletionSource<TSuccess>();

        action(
            argument1,
            argument2,
            argument3,
            argument4,
            modelData => tcs.SetResult(modelData),
            errorData => tcs.SetException(errorData.ToException())
        );

        return tcs.Task;
    }
}

