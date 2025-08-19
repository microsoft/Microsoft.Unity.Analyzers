/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class CodeStyleSuppressorTests : BaseSuppressorVerifierTest<CodeStyleSuppressor>
{
	public static readonly AnalyzerVerificationContext Context = AnalyzerVerificationContext.Default
		.WithAnalyzerOption("dotnet_naming_style.camel_case.capitalization", "camel_case")
		.WithAnalyzerOption("dotnet_naming_symbols.private_methods.applicable_kinds", "method")
		.WithAnalyzerOption("dotnet_naming_symbols.private_methods.applicable_accessibilities", "private")
		.WithAnalyzerOption("dotnet_naming_rule.private_methods.symbols", "private_methods")
		.WithAnalyzerOption("dotnet_naming_rule.private_methods.style", "camel_case")
		.WithAnalyzerOption("dotnet_naming_rule.private_methods.severity", "warning");

	[Fact]
	public async Task TestCodeStyleIgnoreForUnityMessages()
	{
		const string test = @"
using UnityEngine;

class Menu : MonoBehaviour
{
    private void Update() { }
    public void Start() { }
}";

		var suppressor = ExpectSuppressor(CodeStyleSuppressor.Rule)
			.WithLocation(6, 18);

		await VerifyCSharpDiagnosticAsync(Context, test, suppressor);
	}

	[Fact]
	public async Task TestCodeStyleEffectiveForNonUnityMessages()
	{
		const string test = @"
using UnityEngine;

class Menu : MonoBehaviour
{
    private void Foo() { }
    public void Bar() { }
}";

		var not = ExpectNotSuppressed(CodeStyleSuppressor.Rule)
				.WithLocation(6, 18)
				.WithSeverity(DiagnosticSeverity.Info)
				.WithMessageFormat("Naming rule violation: The first word, '{0}', must begin with a lower case character")
				.WithArguments("Foo");

		await VerifyCSharpDiagnosticAsync(Context, test, not);
	}

	[Fact]
	public async Task TestCodeStyleIgnoreForUnityMessagesAlt()
	{
		const string test = @"
using UnityEngine;

class Menu : ScriptableObject
{
    private void Awake() { }
}";

		var suppressor = ExpectSuppressor(CodeStyleSuppressor.Rule)
			.WithLocation(6, 18);

		await VerifyCSharpDiagnosticAsync(Context, test, suppressor);
	}

	[Fact]
	public async Task TestCodeStyleEffectiveForNonMessageBasedTypes()
	{
		const string test = @"
class Bar
{
    private void Foo() { }
}";

		var not = ExpectNotSuppressed(CodeStyleSuppressor.Rule)
			.WithLocation(4, 18)
			.WithSeverity(DiagnosticSeverity.Info)
			.WithMessageFormat("Naming rule violation: The first word, '{0}', must begin with a lower case character")
			.WithArguments("Foo");

		await VerifyCSharpDiagnosticAsync(Context, test, not);
	}

}
