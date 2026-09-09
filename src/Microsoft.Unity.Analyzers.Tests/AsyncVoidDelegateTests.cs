/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class AsyncVoidDelegateTests : BaseDiagnosticVerifierTest<AsyncVoidDelegateAnalyzer>
{
	[Theory]
	[InlineData("Action callback = async () => { await Awaitable.NextFrameAsync(); }; callback();")]
	[InlineData("Action<int> callback = async value => { await Task.Yield(); }; callback(1);")]
	[InlineData("Action callback = async delegate { await Task.Yield(); }; callback();")]
	[InlineData("Action<int> callback = async delegate(int value) { await Task.Yield(); }; callback(1);")]
	[InlineData("Action callback = async () => await Task.Delay(1); callback();")]
	[InlineData("Action callback = async () => { await default(UniTask); }; callback();")]
	[InlineData("Action callback = async () => { await default(UniTask<int>); }; callback();")]
	[InlineData("UnityEngine.Events.UnityAction callback = async () => { await Task.Yield(); }; callback();")]
	[InlineData("EventHandler callback = async (sender, args) => { await Task.Yield(); }; callback(null, EventArgs.Empty);")]
	[InlineData("Register(async () => { await Task.Yield(); });")]
	[InlineData("Register((Action)(async () => { await Task.Yield(); }));")]
	public async Task VoidReturningDelegates(string statement)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, CreateSource(statement),
			ExpectDiagnostic().WithLocation(11, 9 + statement.IndexOf("async", StringComparison.Ordinal)));
	}

	[Theory]
	[InlineData("Task", "")]
	[InlineData("Task<int>", "return 1;")]
	[InlineData("ValueTask", "")]
	[InlineData("ValueTask<int>", "return 1;")]
	[InlineData("Awaitable", "")]
	[InlineData("Awaitable<int>", "return 1;")]
	[InlineData("UniTask", "")]
	[InlineData("UniTask<int>", "return 1;")]
	[InlineData("UniTaskVoid", "")]
	public async Task TaskReturningDelegates(string type, string result)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context,
			CreateSource($"Func<{type}> callback = async () => {{ await Task.Yield(); {result} }}; _ = callback();"));
	}

	[Theory]
	[InlineData("Action callback = () => { }; callback();")]
	[InlineData("Action callback = delegate { }; callback();")]
	[InlineData("_ = Task.Run(async () => { await Task.Yield(); });")]
	[InlineData("Func<Awaitable> callback = () => Awaitable.NextFrameAsync(); _ = callback();")]
	[InlineData("Func<UniTask> callback = () => default; _ = callback();")]
	public async Task NoAsyncVoidConversion(string statement)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, CreateSource(statement));
	}

	[Fact]
	public async Task EventSubscription()
	{
		const string source = @"
using System;
using System.Threading.Tasks;

class Example
{
    public event Action Changed;
    public void Configure()
    {
        Changed += async () => { await Task.Yield(); };
        Changed?.Invoke();
    }
}
";

		await VerifyCSharpDiagnosticAsync(source, ExpectDiagnostic().WithLocation(10, 20));
	}

	[Fact]
	public async Task NestedAsyncVoidDelegate()
	{
		var source = CreateSource(@"_ = Task.Run(async () =>
        {
            Action callback = async () => { await Task.Yield(); };
            callback();
            await Task.Yield();
        });");

		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source,
			ExpectDiagnostic().WithLocation(13, 31));
	}

	[Fact]
	public async Task MethodGroupIsNotAnAnonymousFunction()
	{
		const string source = @"
using System;
using System.Threading.Tasks;

class Example
{
    public void Configure()
    {
        Action callback = Run;
        callback();
    }
    private async void Run() { await Task.Yield(); }
}
";

		await VerifyCSharpDiagnosticAsync(source);
	}

	private static string CreateSource(string statement)
	{
		return $@"
using System;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;

class Example
{{
    public void Configure()
    {{
        {statement}
    }}
    private void Register(Action callback) {{ callback(); }}
}}
" + AsyncTestSources.UniTaskTypes;
	}
}
