/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class Vector3ConversionTests : BaseCodeFixVerifierTest<Vector3ConversionAnalyzer, Vector3ConversionCodeFix>
{
	[Fact]
	public async Task UseConversionTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Vector3 v3 = Vector3.zero;
        Vector2 v2 = Vector2.zero;
        var distance = Vector2.Distance(v2, new Vector2(v3.x, v3.y));
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 45);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Vector3 v3 = Vector3.zero;
        Vector2 v2 = Vector2.zero;
        var distance = Vector2.Distance(v2, v3);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task DoNotUseConversionWithoutSameIdentifierTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Vector3 v3 = Vector3.zero;
        Vector3 v3b = Vector3.zero;
        Vector2 v2 = Vector2.zero;
        var distance = Vector2.Distance(v2, new Vector2(v3.x, v3b.y));
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task NoAmbiguityAfterConversionTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Vector3 v3 = Vector3.zero;
        Vector2 v2 = Vector2.zero;
        var test = new Vector2(v3.x, v3.y);
        var result = v2 - test;
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 20);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Vector3 v3 = Vector3.zero;
        Vector2 v2 = Vector2.zero;
        var test = (Vector2)v3;
        var result = v2 - test;
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task NoAmbiguousMethodCallTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Foo(Vector3 bar) {
    }

    void Foo(Vector2 bar) {
    }

    void Update()
    {
        Vector3 v3 = Vector3.zero;
        Foo(new Vector2(v3.x, v3.y));
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}
}
