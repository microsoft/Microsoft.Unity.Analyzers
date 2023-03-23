/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class Vector2ConversionTests : BaseCodeFixVerifierTest<Vector2ConversionAnalyzer, Vector2ConversionCodeFix>
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
        var distance = Vector3.Distance(v3, new Vector3(v2.x, v2.y, 0));
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
        var distance = Vector3.Distance(v3, v2);
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
        Vector2 v2 = Vector2.zero;
        Vector2 v2b = Vector2.zero;
        var distance = Vector3.Distance(v3, new Vector3(v2.x, v2b.y, 0));
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task DoNotUseConversionWithoutNonZeroZTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Vector3 v3 = Vector3.zero;
        Vector2 v2 = Vector2.zero;
        var distance = Vector3.Distance(v3, new Vector3(v2.x, v2.y, 1));
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
        var test = new Vector3(v2.x, v2.y, 0);
        var result = v3 - test;
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
        var test = (Vector3)v2;
        var result = v3 - test;
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
