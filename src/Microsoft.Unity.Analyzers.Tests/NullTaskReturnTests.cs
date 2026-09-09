/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class NullTaskReturnTests : BaseDiagnosticVerifierTest<NullTaskReturnAnalyzer>
{
	public static TheoryData<string, string> NullReturns
	{
		get
		{
			var data = new TheoryData<string, string>();
			foreach (var type in new[] { "Task", "Task<int>", "Awaitable", "Awaitable<int>" })
			{
				foreach (var expression in new[]
				{
					"null", "default", $"default({type})", $"({type})null", "null!",
					"condition ? other : null", "condition ? default : other",
					"condition switch { true => other, _ => null }", "other ?? null"
				})
					data.Add(type, expression);
			}

			return data;
		}
	}

	[Theory]
	[MemberData(nameof(NullReturns))]
	public async Task ReturnsNullTask(string type, string expression)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, CreateSource(type, expression),
			ExpectDiagnostic().WithLocation(11, 16));
	}

	[Theory]
	[InlineData("Task")]
	[InlineData("Task<int>")]
	[InlineData("Awaitable")]
	[InlineData("Awaitable<int>")]
	public async Task ExpressionBodiedMethod(string type)
	{
		var source = CreateSource(type, "other")
			.Replace(@"
    {
        return other;
    }", " => null;");

		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source,
			ExpectDiagnostic().WithLocation(9, 45 + 2 * type.Length));
	}

	[Fact]
	public async Task PropertiesAndGetters()
	{
		const string source = @"
using UnityEngine;

class Example
{
    public Awaitable Operation => null;
    public Awaitable Other { get { return default; } }
    public Awaitable this[int index] => default;
}
";
		await VerifyCSharpDiagnosticAsync(source,
			ExpectDiagnostic().WithLocation(6, 35),
			ExpectDiagnostic().WithLocation(7, 43),
			ExpectDiagnostic().WithLocation(8, 41));
	}

	[Fact]
	public async Task NestedSynchronousFunctionsInsideAsyncMethod()
	{
		const string source = @"
using System;
using System.Threading.Tasks;
using UnityEngine;

class Example
{
    public async Task Run()
    {
        Func<Awaitable> first = () => null;
        Func<Awaitable> second = delegate { return default; };
        Awaitable Read() => null;
        _ = first();
        _ = second();
        _ = Read();
        await Task.Yield();
    }
}
";
		await VerifyCSharpDiagnosticAsync(source,
			ExpectDiagnostic().WithLocation(10, 39),
			ExpectDiagnostic().WithLocation(11, 52),
			ExpectDiagnostic().WithLocation(12, 29));
	}

	[Theory]
	[InlineData("Task<string>")]
	[InlineData("ValueTask<string>")]
	[InlineData("Awaitable<string>")]
	[InlineData("UniTask<string>")]
	[InlineData("Task<Task>")]
	[InlineData("Awaitable<Awaitable>")]
	public async Task AsyncNullResultIsNotANullTask(string type)
	{
		var source = CreateSource(type, "null")
			.Replace($"public {type} Read", $"public async {type} Read")
			.Replace("return null;", "await Task.Yield(); return null;");

		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source);
	}

	[Fact]
	public async Task NestedAsyncNullResults()
	{
		const string source = @"
using System;
using System.Threading.Tasks;

class Example
{
    public void Run()
    {
        Func<Task<Task>> read = async () => { await Task.Yield(); return null; };
        async Task<Task> Local() { await Task.Yield(); return null; }
        _ = read();
        _ = Local();
    }
}
";
		await VerifyCSharpDiagnosticAsync(source);
	}

	[Theory]
	[InlineData("ValueTask")]
	[InlineData("ValueTask<int>")]
	[InlineData("UniTask")]
	[InlineData("UniTask<int>")]
	[InlineData("UniTaskVoid")]
	[InlineData("UniTask?")]
	public async Task ValueTypeDefaultsAreNotNullTasks(string type)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, CreateSource(type, "default"));
	}

	[Theory]
	[InlineData("other")]
	[InlineData("Task.CompletedTask")]
	[InlineData("null ?? other")]
	[InlineData("other ?? Task.CompletedTask")]
	[InlineData("condition ? other : Task.CompletedTask")]
	[InlineData("true ? other : null")]
	[InlineData("false ? null : other")]
	[InlineData("condition switch { true => other, _ => Task.CompletedTask }")]
	public async Task NonNullReturnExpressions(string expression)
	{
		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, CreateSource("Task", expression));
	}

	[Fact]
	public async Task NullableTaskStillCannotBeAwaitedWhenNull()
	{
		const string source = @"
#nullable enable
using UnityEngine;

class Example
{
    public Awaitable? Read() => null;
}
";
		await VerifyCSharpDiagnosticAsync(source, ExpectDiagnostic().WithLocation(7, 33));
	}

	[Fact]
	public async Task ConditionalAccessAndCoalescing()
	{
		const string source = @"
using UnityEngine;

class Example
{
    public Awaitable Read() => Awaitable.NextFrameAsync();
    public Awaitable MaybeRead(Example other) => other?.Read();
    public Awaitable ReadOrFallback(Example other) => other?.Read() ?? Read();
}
";
		await VerifyCSharpDiagnosticAsync(source, ExpectDiagnostic().WithLocation(7, 50));
	}

	[Fact]
	public async Task UserDefinedConversionMayReturnANonNullTask()
	{
		const string source = @"
using UnityEngine;

class Example
{
    public static implicit operator Awaitable(Example value) => Awaitable.NextFrameAsync();
    public Awaitable Read() => (Example)null;
}
";
		await VerifyCSharpDiagnosticAsync(source);
	}

	[Fact]
	public async Task NonTaskReturnTypes()
	{
		const string source = @"
using System.Threading.Tasks;

class Example
{
    public object Read() => (Task)null;
    public string Text() => null;
    public Other.Awaitable Custom() => null;
    public void Run() { return; }
}

namespace Other
{
    class Awaitable { }
}
";
		await VerifyCSharpDiagnosticAsync(source);
	}

	[Theory]
	[InlineData("Read<T>(T other, bool condition) where T : Task")]
	[InlineData("Read<T, U>(T other, bool condition) where T : U where U : Task")]
	public async Task ConstrainedTypeParameter(string signature)
	{
		var source = CreateSource("T", "null")
			.Replace("Read(T other, bool condition)", signature);

		await VerifyCSharpDiagnosticAsync(AsyncTestSources.Context, source,
			ExpectDiagnostic().WithLocation(11, 16));
	}

	private static string CreateSource(string type, string expression)
	{
		return $@"
using System;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;

class Example
{{
    public {type} Read({type} other, bool condition)
    {{
        return {expression};
    }}
}}
" + AsyncTestSources.UniTaskTypes;
	}
}
