/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class NonGenericGetComponentTests : BaseTestCodeFixVerifier<NonGenericGetComponentAnalyzer, NonGenericGetComponentCodeFix>
	{
		[Fact]
		public void GetComponentAs()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent(typeof(Rigidbody)) as Rigidbody;
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(10, 14)
				.WithArguments("GetComponent");

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void CastGetComponent()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	private Rigidbody rb;

    private void Start()
    {
        rb = (Rigidbody)GetComponent(typeof(Rigidbody));
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(10, 25)
				.WithArguments("GetComponent");

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void GetComponentsInChildrenBoolean()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        GetComponentsInChildren(typeof(Rigidbody), true);
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 9)
				.WithArguments("GetComponentsInChildren");

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        GetComponentsInChildren<Rigidbody>(true);
    }
}
";
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void GetComponentTypeVariable()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        var t = typeof(Rigidbody);
        GetComponent(t);
    }
}
";

			// We're assuming that using GetComponent with anything else than a typeof
			// argument is from a computation that makes it impossible to use the generic form
			VerifyCSharpDiagnostic(test);
		}

		[Fact]
		public void GetComponentGeneric()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Rigidbody>();
    }
}
";

			// Verify we're not misreporting an already generic usage
			VerifyCSharpDiagnostic(test);
		}
	}
}
