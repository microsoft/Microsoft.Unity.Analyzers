/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class SetPixelsTests : BaseDiagnosticVerifierTest<SetPixelsAuditAnalyzer>
	{
		[Fact]
		public async Task Texture2DTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Test(Texture2D test)
    {
        test.SetPixels(null);
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 9)
				.WithArguments("SetPixels");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);
		}

		[Fact]
		public async Task Texture3DTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Test(Texture3D test)
    {
        test.SetPixels(null);
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 9)
				.WithArguments("SetPixels");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);
		}

		[Fact]
		public async Task CubemapArrayTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Test(CubemapArray test)
    {
        test.SetPixels(null, CubemapFace.Unknown, 0);
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 9)
				.WithArguments("SetPixels");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);
		}

		[Fact]
		public async Task Texture2DArrayTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Test(Texture2DArray test)
    {
        test.SetPixels(null, 0);
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 9)
				.WithArguments("SetPixels");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);
		}

	}
}
