/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class AsyncVoidMethodTests : BaseDiagnosticVerifierTest<AsyncVoidMethodAnalyzer>
{
	[Theory]
	[InlineData("Task.Yield()")]
	[InlineData("Awaitable.NextFrameAsync()")]
	[InlineData("default(UniTask)")]
	[InlineData("default(UniTask<int>)")]
	public async Task AsyncVoidMethod(string awaited)
	{
		var source = CreateSource($"public async void Run() {{ await {awaited}; }}");
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source,
			ExpectDiagnostic().WithLocation(9, 23).WithArguments("Run"));
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
	public async Task TaskReturningMethod(string type, string result)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context,
			CreateSource($"public async {type} Run() {{ await Task.Yield(); {result} }}"));
	}

	[Fact]
	public async Task SynchronousVoidMethod()
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, CreateSource("public void Run() { }"));
	}

	[Theory]
	[InlineData("MonoBehaviour", "public", "Start", "")]
	[InlineData("MonoBehaviour", "public", "Awake", "")]
	[InlineData("MonoBehaviour", "public", "OnEnable", "")]
	[InlineData("MonoBehaviour", "public", "Update", "")]
	[InlineData("MonoBehaviour", "public", "OnApplicationPause", "bool pause")]
	[InlineData("MonoBehaviour", "public", "OnApplicationPause", "")]
	[InlineData("ScriptableObject", "public", "OnEnable", "")]
	[InlineData("UnityEditor.EditorWindow", "public", "OnGUI", "")]
	[InlineData("UnityEditor.AssetPostprocessor", "public static", "OnPostprocessAllAssets", "")]
	public async Task UnityCallbacks(string baseType, string modifiers, string name, string parameters)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context,
			CreateSource($"{modifiers} async void {name}({parameters}) {{ await Task.Yield(); }}", baseType));
	}

	[Theory]
	[InlineData("", "public async void Start()", 23)]
	[InlineData("MonoBehaviour", "public static async void Start()", 30)]
	[InlineData("MonoBehaviour", "public async void Start(int argument)", 23)]
	[InlineData("MonoBehaviour", "public async void Start<T>()", 23)]
	public async Task UnityMessageNameAloneIsNotACallback(string baseType, string declaration, int column)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context,
			CreateSource($"{declaration} {{ await Task.Yield(); }}", baseType),
			ExpectDiagnostic().WithLocation(9, column).WithArguments("Start"));
	}

	[Fact]
	public async Task LocalFunctionIsNotAUnityMessage()
	{
		var source = CreateSource(@"public void Run()
    {
        async void Start() { await Task.Yield(); }
        Start();
    }", "MonoBehaviour");

		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source,
			ExpectDiagnostic().WithLocation(11, 20).WithArguments("Start"));
	}

	[Fact]
	public async Task ExpressionBodiedMethodAndLocalFunction()
	{
		var source = CreateSource(@"public async void Run() => await Task.Yield();
    public void Configure()
    {
        async void Local() => await Task.Yield();
        Local();
    }");

		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source,
			ExpectDiagnostic().WithLocation(9, 23).WithArguments("Run"),
			ExpectDiagnostic().WithLocation(12, 20).WithArguments("Local"));
	}

	[Theory]
	[InlineData("object sender, EventArgs args")]
	[InlineData("object sender, System.ComponentModel.CancelEventArgs args")]
	public async Task EventHandlerSignature(string parameters)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context,
			CreateSource($"public async void Changed({parameters}) {{ await Task.Yield(); }}"));
	}

	[Theory]
	[InlineData("string sender, EventArgs args")]
	[InlineData("object sender, object args")]
	[InlineData("EventArgs args")]
	public async Task OtherParametersAreNotAnEventHandler(string parameters)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context,
			CreateSource($"public async void Changed({parameters}) {{ await Task.Yield(); }}"),
			ExpectDiagnostic().WithLocation(9, 23).WithArguments("Changed"));
	}

	[Fact]
	public async Task OverrideAndInterfaceContracts()
	{
		const string source = @"
using System.Threading.Tasks;

interface IHandler
{
    void Handle();
    void HandleExplicitly();
}

class Base
{
    public virtual void Run() { }
}

class Example : Base, IHandler
{
    public override async void Run() { await Task.Yield(); }
    public async void Handle() { await Task.Yield(); }
    async void IHandler.HandleExplicitly() { await Task.Yield(); }
}
";

		await VerifyCSharpDiagnosticAsync(source);
	}

	[Fact]
	public async Task DeclaringAVirtualMethodStillWarns()
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context,
			CreateSource("public virtual async void Run() { await Task.Yield(); }"),
			ExpectDiagnostic().WithLocation(9, 31).WithArguments("Run"));
	}

	[Theory]
	[InlineData("[RuntimeInitializeOnLoadMethod] public static async void Initialize()")]
	[InlineData("[UnityEditor.InitializeOnLoadMethod] public static async void Initialize()")]
	[InlineData("[UnityEditor.Callbacks.DidReloadScripts] public static async void Initialize()")]
	[InlineData("[ContextMenu(\"Initialize\")] public async void Initialize()")]
	[InlineData("[UnityEditor.MenuItem(\"Tests/Initialize\")] public static async void Initialize()")]
	public async Task AttributedUnityCallbacks(string declaration)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context,
			CreateSource($"{declaration} {{ await Task.Yield(); }}", "MonoBehaviour"));
	}

	private static string CreateSource(string members, string baseType = "")
	{
		var inheritance = baseType.Length == 0 ? "" : " : " + baseType;
		return $@"
using System;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;

class Example{inheritance}
{{
    {members}
}}
" + AsyncTestSources.UniTaskTypes;
	}
}
