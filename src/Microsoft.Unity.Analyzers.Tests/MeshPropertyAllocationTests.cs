/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class MeshPropertyAllocationTests : BaseCodeFixVerifierTest<MeshPropertyAllocationAnalyzer, MeshPropertyAllocationCodeFix>
{
	[Fact]
	public async Task Test()
	{
		const string test = @"
class DummyClass
{
    private void Test(UnityEngine.Mesh mesh) {
        for (var i = 0; i <= 5; i++) {
            var c = mesh.uv;
        }
    }
}
";

		var diagnostic = ExpectDiagnostic(MeshPropertyAllocationAnalyzer.Rule)
			.WithLocation(6, 21)
			.WithArguments("uv");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}
}
