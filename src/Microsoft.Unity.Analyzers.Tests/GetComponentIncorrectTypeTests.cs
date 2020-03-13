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
		public void GetComponentInterfaceTypeTest()
		{
			const string test = @"
using System;
using UnityEngine;

class Camera : MonoBehaviour
{
    private IDisposable disp;

    private void Start()
    {
        disp = GetComponent<IDisposable>();
    }
}
";

			VerifyCSharpDiagnostic(test);
		}

		[Fact]
		public void GetComponentCorrectTypeTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
}
";

			VerifyCSharpDiagnostic(test);
		}

		[Fact]
		public void GetComponentIncorrectTypeTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private int i;

    private void Start()
    {
        i = GetComponent<int>();
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(10, 13)
				.WithArguments("Int32");

			VerifyCSharpDiagnostic(test, diagnostic);
		}

		[Fact]
		public void GetComponentLegacyTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        var hello = GetComponent(""Hello"");
    }
}
";

			VerifyCSharpDiagnostic(test);
		}
	}
}
