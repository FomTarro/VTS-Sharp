using System;
using System.Threading.Tasks;

namespace VTS.Core {
    internal static class VTSExtensions {
        internal static VTSException ToException(this VTSErrorData errorData) {
            return new VTSException(errorData);
        }

        internal static Task<TSuccess> Async<TSuccess, TError>(this Action<Action<TSuccess>, Action<TError>> action)
            where TError : VTSErrorData {
            var tcs = new TaskCompletionSource<TSuccess>();

            action(
                modelData => tcs.SetResult(modelData),
                errorData => tcs.SetException(errorData.ToException())
            );

            return tcs.Task;
        }

        internal static Task<TSuccess> Async<T1, TSuccess, TError>(this Action<T1, Action<TSuccess>, Action<TError>> action,
            T1 argument1) where TError : VTSErrorData {
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
            where TError : VTSErrorData {
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
            where TError : VTSErrorData {
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
            T4 argument4) where TError : VTSErrorData {
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

        internal static Task<TSuccess> Async<T1, T2, T3, T4, T5, TSuccess, TError>(
            this Action<T1, T2, T3, T4, T5, Action<TSuccess>, Action<TError>> action, T1 argument1, T2 argument2, T3 argument3,
            T4 argument4, T5 argument5) where TError : VTSErrorData {
            var tcs = new TaskCompletionSource<TSuccess>();

            action(
                argument1,
                argument2,
                argument3,
                argument4,
                argument5,
                modelData => tcs.SetResult(modelData),
                errorData => tcs.SetException(errorData.ToException())
            );

            return tcs.Task;
        }

        internal static Task<TSuccess> Async<T1, T2, T3, T4, T5, T6, TSuccess, TError>(
            this Action<T1, T2, T3, T4, T5, T6, Action<TSuccess>, Action<TError>> action, T1 argument1, T2 argument2, T3 argument3,
            T4 argument4, T5 argument5, T6 argument6) where TError : VTSErrorData {
            var tcs = new TaskCompletionSource<TSuccess>();

            action(
                argument1,
                argument2,
                argument3,
                argument4,
                argument5,
                argument6,
                modelData => tcs.SetResult(modelData),
                errorData => tcs.SetException(errorData.ToException())
            );

            return tcs.Task;
        }

        internal static Task<TSuccess> Async<T1, T2, T3, T4, T5, T6, T7, TSuccess, TError>(
            this Action<T1, T2, T3, T4, T5, T6, T7, Action<TSuccess>, Action<TError>> action, T1 argument1, T2 argument2, T3 argument3,
            T4 argument4, T5 argument5, T6 argument6, T7 argument7) where TError : VTSErrorData {
            var tcs = new TaskCompletionSource<TSuccess>();

            action(
                argument1,
                argument2,
                argument3,
                argument4,
                argument5,
                argument6,
                argument7,
                modelData => tcs.SetResult(modelData),
                errorData => tcs.SetException(errorData.ToException())
            );

            return tcs.Task;
        }

        internal static Task<TSuccess> Async<T1, T2, T3, T4, T5, T6, T7, T8, TSuccess, TError>(
            this Action<T1, T2, T3, T4, T5, T6, T7, T8, Action<TSuccess>, Action<TError>> action, T1 argument1, T2 argument2, T3 argument3,
            T4 argument4, T5 argument5, T6 argument6, T7 argument7, T8 argument8) where TError : VTSErrorData {
            var tcs = new TaskCompletionSource<TSuccess>();

            action(
                argument1,
                argument2,
                argument3,
                argument4,
                argument5,
                argument6,
                argument7,
                argument8,
                modelData => tcs.SetResult(modelData),
                errorData => tcs.SetException(errorData.ToException())
            );

            return tcs.Task;
        }
    }
}

