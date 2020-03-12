/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class CacheExpensiveCallsTests : BaseCodeFixVerifierTest<CacheExpensiveCallsAnalyzer, CacheExpensiveCallsCodeFix>
	{
		[Fact]
		public void TestTest()
		{
			const string test = @"
using UnityEngine;

class NewBehaviour : MonoBehaviour
{
    private Camera camera;

	private void Update()
    {
        camera = Camera.main;
    }
}
";


			var diagnostic = ExpectDiagnostic()
				.WithLocation(10, 18)
				.WithArguments("Camera.main");

			VerifyCSharpDiagnostic(test, diagnostic);

//			const string fixedTest = @"
//using UnityEngine;

//class Test : MonoBehaviour
//{
//}
//";

			//VerifyCSharpFix(test, fixedTest);
		}
	}
}
