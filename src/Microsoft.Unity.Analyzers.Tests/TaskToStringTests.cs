/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class TaskToStringTests : BaseDiagnosticVerifierTest<TaskToStringAnalyzer>
{
	[Theory]
	[InlineData("Task")]
	[InlineData("Task<int>")]
	[InlineData("ValueTask")]
	[InlineData("ValueTask<int>")]
	[InlineData("Awaitable")]
	[InlineData("Awaitable<int>")]
	[InlineData("UniTask")]
	[InlineData("UniTask<int>")]
	public async Task SupportedTaskTypes(string type)
	{
		string[] statements =
		[
			"_ = $\"Result: {value}\";",
			"_ = \"Result: \" + value;",
			"_ = value + \" result\";",
			"_ = value.ToString();",
			"Debug.Log(value);",
		];

		var expected = statements.Select((statement, index) =>
			ExpectDiagnostic().WithLocation(12 + index, 9 + statement.IndexOf("value", StringComparison.Ordinal))).ToArray();

		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context,
			CreateSource(type, string.Join("\n        ", statements)), expected);
	}

	[Theory]
	[InlineData("_ = $\"{value,10}\";")]
	[InlineData("_ = $\"{value:format}\";")]
	[InlineData("_ = $\"{(value)}\";")]
	[InlineData("_ = string.Format(\"{0}\", value);")]
	[InlineData("_ = string.Format(arg0: value, format: \"{0}\");")]
	[InlineData("_ = string.Format(System.Globalization.CultureInfo.InvariantCulture, \"{0}\", value);")]
	[InlineData("_ = string.Format(\"{0} {1} {2} {3}\", 1, 2, 3, value);")]
	[InlineData("_ = string.Format(\"{0}\", new object[] { value });")]
	[InlineData("_ = string.Concat(\"Result: \", value);")]
	[InlineData("_ = string.Concat(new object[] { \"Result: \", value });")]
	[InlineData("_ = new StringBuilder().Append(value);")]
	[InlineData("_ = new StringBuilder().AppendFormat(\"{0}\", value);")]
	[InlineData("Console.Write(value);")]
	[InlineData("Console.WriteLine(\"{0}\", value);")]
	[InlineData("Debug.Log(value, null);")]
	[InlineData("Debug.LogWarning(value);")]
	[InlineData("Debug.LogError(value);")]
	[InlineData("Debug.LogFormat(\"{0}\", value);")]
	[InlineData("Debug.LogWarningFormat(\"{0}\", value);")]
	[InlineData("Debug.LogErrorFormat(null, \"{0}\", value);")]
	public async Task StringContexts(string statement)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, CreateSource("Awaitable<int>", statement),
			ExpectDiagnostic().WithLocation(12, 9 + statement.IndexOf("value", StringComparison.Ordinal)));
	}

	[Fact]
	public async Task CompoundConcatenation()
	{
		var source = CreateSource("UniTask<int>", @"string text = """";
        text += value;
        Debug.Log(text);");

		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source,
			ExpectDiagnostic().WithLocation(13, 17));
	}

	[Fact]
	public async Task MultipleFormattingArguments()
	{
		var source = CreateSource("UniTask<int>", "Debug.LogFormat(\"{0} {1}\", value, value);");
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source,
			ExpectDiagnostic().WithLocation(12, 36),
			ExpectDiagnostic().WithLocation(12, 43));
	}

	[Fact]
	public async Task ToStringInsideInterpolationIsReportedOnce()
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context,
			CreateSource("Task<int>", "_ = $\"{value.ToString()}\";"),
			ExpectDiagnostic().WithLocation(12, 16));
	}

	[Fact]
	public async Task TaskSubclass()
	{
		var source = CreateSource("DerivedTask", "_ = $\"{value}\";") + @"
class DerivedTask : System.Threading.Tasks.Task<int>
{
    public DerivedTask() : base(() => 1) { }
}
";
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source,
			ExpectDiagnostic().WithLocation(12, 16));
	}

	[Theory]
	[InlineData("Run<T>(T value) where T : Task")]
	[InlineData("Run<T, U>(T value) where T : U where U : Task")]
	public async Task ConstrainedTypeParameter(string signature)
	{
		var source = CreateSource("T", "_ = $\"{value}\";")
			.Replace("Run(T value)", signature);

		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source,
			ExpectDiagnostic().WithLocation(12, 16));
	}

	[Theory]
	[InlineData("Task<int>")]
	[InlineData("ValueTask<int>")]
	[InlineData("Awaitable<int>")]
	[InlineData("UniTask<int>")]
	public async Task AwaitedResult(string type)
	{
		var source = CreateSource(type, "_ = $\"{await value}\";").Replace("void Run(", "async Task Run(");
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source);
	}

	[Theory]
	[InlineData("_ = value;")]
	[InlineData("object boxed = value; Debug.Log(boxed);")]
	[InlineData("_ = (object)value;")]
	[InlineData("_ = $\"{(object)value}\";")]
	[InlineData("_ = $\"{value.Status}\";")]
	[InlineData("_ = $\"{value.GetType()}\";")]
	[InlineData("Accept(value);")]
	[InlineData("_ = Format(value);")]
	[InlineData("_ = string.Equals(value, null);")]
	public async Task NotAStringConversion(string statement)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, CreateSource("Task", statement));
	}

	[Theory]
	[InlineData("UniTaskVoid", "")]
	[InlineData("Other.Task", "namespace Other { class Task { } }")]
	[InlineData("Other.ValueTask", "namespace Other { struct ValueTask { } }")]
	[InlineData("Other.UniTask<int>", "namespace Other { struct UniTask<T> { } }")]
	[InlineData("Other.Awaitable", "namespace Other { class Awaitable { } }")]
	[InlineData("UnityEngine.Container.Awaitable", "namespace UnityEngine { class Container { public class Awaitable { } } }")]
	[InlineData("UnityEngine.Awaitable<int, int>", "namespace UnityEngine { class Awaitable<T, U> { } }")]
	public async Task NonTaskTypes(string type, string declaration)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context,
			CreateSource(type, "_ = $\"{value}\";") + declaration);
	}

	[Fact]
	public async Task UserDefinedConcatenation()
	{
		var source = CreateSource("DerivedTask", "_ = \"Result: \" + value;") + @"
class DerivedTask : System.Threading.Tasks.Task
{
    public DerivedTask() : base(() => { }) { }
    public static string operator +(string text, DerivedTask task) => text;
}
";
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source);
	}

	[Fact]
	public async Task ToStringMethodReturningAnotherType()
	{
		var source = CreateSource("DerivedTask", "_ = value.ToString();") + @"
class DerivedTask : System.Threading.Tasks.Task
{
    public DerivedTask() : base(() => { }) { }
    public new int ToString() => 1;
}
";
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source);
	}

	private static string CreateSource(string type, string statement)
	{
		return $@"
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;

class Example
{{
    public void Run({type} value)
    {{
        {statement}
    }}
    private static void Accept(object value) {{ }}
    private static string Format(object value) => """";
}}
" + AsyncTestSources.UniTaskTypes;
	}
}
