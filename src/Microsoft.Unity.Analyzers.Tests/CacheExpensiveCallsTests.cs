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

class Camera : MonoBehaviour
{
    private void Update()
    {
        GetComponent<Rigidbody>();
    }
}
";


			var diagnostic = ExpectDiagnostic().WithLocation(8, 9);

			//VerifyCSharpDiagnostic(test, diagnostic);

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
