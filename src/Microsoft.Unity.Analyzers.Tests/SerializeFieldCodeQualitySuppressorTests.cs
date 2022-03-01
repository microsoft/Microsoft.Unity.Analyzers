/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class SerializeFieldCodeQualitySuppressorTests : BaseSuppressorVerifierTest<SerializeFieldSuppressor>
{
	// Only load CodeQuality analyzers for those tests
	protected override SuppressorVerifierAnalyzers SuppressorVerifierAnalyzers => SuppressorVerifierAnalyzers.CodeQuality;

	[Fact]
	public async Task PrivateFieldWithAttributeUnusedSuppressed()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    string someField = ""default"";
}
";

		var suppressor = ExpectSuppressor(SerializeFieldSuppressor.UnusedCodeQualityRule)
			.WithLocation(7, 12);

		await VerifyCSharpDiagnosticAsync(test, suppressor);
	}
}
