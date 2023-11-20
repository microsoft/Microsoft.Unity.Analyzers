/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class MessageCodeQualitySuppressorTests : BaseSuppressorVerifierTest<MessageSuppressor>
{
	// Only load CodeQuality analyzers for those tests
	protected override SuppressorVerifierAnalyzers SuppressorVerifierAnalyzers => SuppressorVerifierAnalyzers.CodeQuality;

	[Fact]
	public async Task StaticMethodSuppressed()
	{
		const string test = @"
using UnityEngine;

public class TestScript : MonoBehaviour
{
    private void Start()
    {
    }
}
";

		var suppressor = ExpectSuppressor(MessageSuppressor.MethodCodeQualityRule)
			.WithLocation(6, 18);

		await VerifyCSharpDiagnosticAsync(test, suppressor);
	}

	[Fact]
	public async Task UnusedParameterSuppressed()
	{
		const string test = @"
using UnityEngine;

public class TestScript : MonoBehaviour
{
    private void OnAnimatorIK(int layerIndex)
    {
        OnAnimatorIK(0);
    }
}
";

		// This CA1801 rule has been deprecated in favor of IDE0060
		// Disable this, as the current NetAnalyzers will not trigger a diagnostic anymore
		// var suppressor = ExpectSuppressor(MessageSuppressor.ParameterCodeQualityRule)
		//   .WithLocation(6, 35);

		var context = AnalyzerVerificationContext
			.Default
			.WithAnalyzerFilter(MessageSuppressor.MethodCodeQualityRule.SuppressedDiagnosticId);

		await VerifyCSharpDiagnosticAsync(context, test /*, suppressor*/);
	}
}
