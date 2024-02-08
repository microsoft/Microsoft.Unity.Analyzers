/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class ThrowExpressionSuppressorTests : BaseSuppressorVerifierTest<ThrowExpressionSuppressor>
{
	protected override SuppressorVerifierAnalyzers SuppressorVerifierAnalyzers => SuppressorVerifierAnalyzers.CodeQuality | SuppressorVerifierAnalyzers.CodeStyle;

	[Fact]
	public async Task SuppressThrowExpressionWithUnityObjects()
	{
		const string test = @"
using System;
using UnityEngine;

class Camera : MonoBehaviour
{
    public MonoBehaviour value;
    public void Method(MonoBehaviour value) {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        this.value = value;
    }
}
";

		var suppressor = ExpectSuppressor(ThrowExpressionSuppressor.Rule)
			.WithLocation(10, 13);

		await VerifyCSharpDiagnosticAsync(test, suppressor);
	}

	[Fact]
	public async Task SuppressThrowExpressionWithUnityObjectsCodeBlock()
	{
		const string test = @"
using System;
using UnityEngine;

class Camera : MonoBehaviour
{
    public MonoBehaviour value;
    public void Method(MonoBehaviour value) {
        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }

        this.value = value;
    }
}
";

		var suppressor = ExpectSuppressor(ThrowExpressionSuppressor.Rule)
			.WithLocation(10, 13);

		await VerifyCSharpDiagnosticAsync(test, suppressor);
	}

	[Fact]
	public async Task DoNotSuppressThrowExpressionWithNonUnityObjects()
	{
		const string test = @"
using System;
using UnityEngine;

class Camera : MonoBehaviour
{
    public object value;
    public void Method(object value) {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        this.value = value;
    }
}
";

		var diagnostic = DiagnosticResult.CompilerWarning(ThrowExpressionSuppressor.Rule.SuppressedDiagnosticId)
			.WithSeverity(DiagnosticSeverity.Info)
			.WithMessage("Null check can be simplified")
			.WithLocation(10, 13);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}
}
