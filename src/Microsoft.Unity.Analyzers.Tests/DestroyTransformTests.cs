/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class DestroyTransformTests : BaseCodeFixVerifierTest<DestroyTransformAnalyzer, DestroyTransformCodeFix>
{
	[Fact]
	public async Task TestValidDestroy()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() {
        Destroy(gameObject);
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task TestDestroy()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() {
        Destroy(transform);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(7, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() {
        Destroy(transform.gameObject);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TestObjectDestroy()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() {
        Object.Destroy(transform, 5);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(7, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() {
        Object.Destroy(transform.gameObject, 5);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TestDestroyImmediate()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() {
        DestroyImmediate(transform);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(7, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() {
        DestroyImmediate(transform.gameObject);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TestObjectDestroyImmediate()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() {
        Object.DestroyImmediate(transform, true);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(7, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() {
        Object.DestroyImmediate(transform.gameObject, true);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}
	
}
