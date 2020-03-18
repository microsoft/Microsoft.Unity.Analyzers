/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class NonGenericGetComponentTests : BaseCodeFixVerifierTest<NonGenericGetComponentAnalyzer, NonGenericGetComponentCodeFix>
	{
		[Fact]
		public async Task GetComponentAsAsync()
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

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

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
			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task CastGetComponentAsync()
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

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

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
			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task GetComponentsInChildrenBooleanAsync()
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

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

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
			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task GetComponentTypeVariableAsync()
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
			await VerifyCSharpDiagnosticAsync(test);
		}

		[Fact]
		public async Task GetComponentGenericAsync()
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
			await VerifyCSharpDiagnosticAsync(test);
		}
	}
}
