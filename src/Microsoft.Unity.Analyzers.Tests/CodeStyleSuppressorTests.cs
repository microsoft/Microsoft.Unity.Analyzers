/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Linq;
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

	public static TheoryData<string> GetUnityMessageBaseTypeNames() => [..
		CodeStyleSuppressor.UnityMessageBaseTypes.Select(x => x.Name)
	];

	[Theory]
	[MemberData(nameof(GetUnityMessageBaseTypeNames))]
	public async Task TestCodeStyleIgnoreForUnityMessages(string baseTypeName)
	{
		string test = $$"""
			using UnityEngine;

			class Menu : {{baseTypeName}}
			{
				private void Awake() { }
				public void OnEnable() { }
			}
		""";

		var suppressor = ExpectSuppressor(CodeStyleSuppressor.Rule)
			.WithLocation(5, 16);

		await VerifyCSharpDiagnosticAsync(Context, test, suppressor);
	}

	[Theory]
	[MemberData(nameof(GetUnityMessageBaseTypeNames))]
	public async Task TestCodeStyleEffectiveForNonUnityMessages(string baseTypeName)
	{
		string test = $$"""
			using UnityEngine;

			class Menu : {{baseTypeName}}
			{
				private void Foo() { }
				public void Bar() { }
			}
		""";

		var not = ExpectNotSuppressed(CodeStyleSuppressor.Rule)
			.WithLocation(5, 16)
			.WithSeverity(DiagnosticSeverity.Info)
			.WithMessageFormat("Naming rule violation: The first word, '{0}', must begin with a lower case character")
			.WithArguments("Foo");

		await VerifyCSharpDiagnosticAsync(Context, test, not);
	}
}
