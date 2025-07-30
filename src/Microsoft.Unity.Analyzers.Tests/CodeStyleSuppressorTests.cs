/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class CodeStyleSuppressorTests : BaseSuppressorVerifierTest<CodeStyleSuppressor>
{
	[Fact]
	public async Task TestCodeStyleIgnore()
	{
		const string test = @"
using UnityEngine;

class Menu : MonoBehaviour
{
    private void Update() { }
    public void Start() { }
}";

		var context = AnalyzerVerificationContext.Default
			.WithAnalyzerOption("dotnet_naming_style.camel_case.capitalization", "camel_case")
			.WithAnalyzerOption("dotnet_naming_symbols.private_methods.applicable_kinds", "method")
			.WithAnalyzerOption("dotnet_naming_symbols.private_methods.applicable_accessibilities", "private")
			.WithAnalyzerOption("dotnet_naming_rule.private_methods.symbols", "private_methods")
			.WithAnalyzerOption("dotnet_naming_rule.private_methods.style", "camel_case")
			.WithAnalyzerOption("dotnet_naming_rule.private_methods.severity", "warning");

		var suppressor = ExpectSuppressor(CodeStyleSuppressor.Rule)
			.WithLocation(6, 18);

		await VerifyCSharpDiagnosticAsync(context, test, suppressor);
	}
}
