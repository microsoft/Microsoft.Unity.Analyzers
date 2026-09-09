/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

namespace Microsoft.Unity.Analyzers.Tests;

internal static class AsyncTestSources
{
	internal static readonly AnalyzerVerificationContext Context = AnalyzerVerificationContext.Default
		.WithAnalyzerFilter("CS8019");

	// Compile-only UniTask shapes. Unity's Awaitable types come from the real Unity assemblies.
	internal const string UniTaskTypes = @"
namespace Cysharp.Threading.Tasks
{
    [System.Runtime.CompilerServices.AsyncMethodBuilder(typeof(UniTaskMethodBuilder))]
    public struct UniTask
    {
        public System.Runtime.CompilerServices.TaskAwaiter GetAwaiter() => System.Threading.Tasks.Task.CompletedTask.GetAwaiter();
    }

    [System.Runtime.CompilerServices.AsyncMethodBuilder(typeof(UniTaskMethodBuilder<>))]
    public struct UniTask<T>
    {
        public System.Runtime.CompilerServices.TaskAwaiter<T> GetAwaiter() => System.Threading.Tasks.Task.FromResult(default(T)).GetAwaiter();
    }

    [System.Runtime.CompilerServices.AsyncMethodBuilder(typeof(UniTaskVoidMethodBuilder))]
    public struct UniTaskVoid { }

    public struct UniTaskMethodBuilder
    {
        public static UniTaskMethodBuilder Create() => default;
        public UniTask Task => default;
        public void SetResult() { }
        public void SetException(System.Exception exception) { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }

    public struct UniTaskMethodBuilder<T>
    {
        public static UniTaskMethodBuilder<T> Create() => default;
        public UniTask<T> Task => default;
        public void SetResult(T result) { }
        public void SetException(System.Exception exception) { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }

    public struct UniTaskVoidMethodBuilder
    {
        public static UniTaskVoidMethodBuilder Create() => default;
        public UniTaskVoid Task => default;
        public void SetResult() { }
        public void SetException(System.Exception exception) { }
        public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion
            where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
    }
}
";
}
