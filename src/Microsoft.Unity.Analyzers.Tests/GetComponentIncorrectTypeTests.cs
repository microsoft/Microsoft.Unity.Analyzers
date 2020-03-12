/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class GetComponentIncorrectTypeTests : BaseDiagnosticVerifierTest<GetComponentIncorrectTypeAnalyzer>
	{
		[Fact]
		public void TestTest()
		{
			const string test = @"
using System.Collections;
using UnityEngine;

class Camera : MonoBehaviour
{
	private Rigidbody rb;

    private void Start()
    {
        GetComponent<IEnumerable>();
    }
}
";

			VerifyCSharpDiagnostic(test);
		}
	}
}
