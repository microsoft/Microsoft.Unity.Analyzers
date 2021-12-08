/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using UnityEngine;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class Vector2MathTests: VectorMathTests<Vector2> {}
public class Vector3MathTests: VectorMathTests<Vector3> {}
public class Vector4MathTests: VectorMathTests<Vector4> {}

public abstract class VectorMathTests<T> : BaseCodeFixVerifierTest<VectorMathAnalyzer, VectorMathCodeFix>
{
	[Fact]
	public async Task AlreadySorted()
	{
		string test = $@"
using UnityEngine;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {typeof(T).Name}.zero;
        var a = 12;
        var b = 42;

        var result = a * b * x;
    }}
}}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task SimpleOrdering()
	{
		string test = $@"
using UnityEngine;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {typeof(T).Name}.zero;
        var a = 12;
        var b = 42;

        var result = b * x * a;
    }}
}}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(11, 22);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		string fixedTest = $@"
using UnityEngine;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {typeof(T).Name}.zero;
        var a = 12;
        var b = 42;

        var result = a * b * x;
    }}
}}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task SimpleOrderingTrivia()
	{
		string test = $@"
using UnityEngine;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {typeof(T).Name}.zero;
        var a = 12;
        var b = 42;

        var result = /* left */ b * x * a /* right */;
    }}
}}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(11, 33);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		string fixedTest = $@"
using UnityEngine;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {typeof(T).Name}.zero;
        var a = 12;
        var b = 42;

        var result = /* left */ a * b * x /* right */;
    }}
}}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task MethodArgument()
	{
		string test = $@"
using UnityEngine;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {typeof(T).Name}.zero;
        var a = 12;
        var b = 42;

        {typeof(T).Name}.zero.Equals(b * x * a);
    }}
}}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(11, 29);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		string fixedTest = $@"
using UnityEngine;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {typeof(T).Name}.zero;
        var a = 12;
        var b = 42;

        {typeof(T).Name}.zero.Equals(a * b * x);
    }}
}}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task Parenthesis()
	{
		string test = $@"
using UnityEngine;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {typeof(T).Name}.zero;
        var a = 12;
        var b = 42;

        var result = x + (b * x * a);
    }}
}}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(11, 27);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		string fixedTest = $@"
using UnityEngine;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {typeof(T).Name}.zero;
        var a = 12;
        var b = 42;

        var result = x + (a * b * x);
    }}
}}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task Multiple()
	{
		string test = $@"
using UnityEngine;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {typeof(T).Name}.zero;
        var a = 12;
        var b = 42;

        var result = b * x * a * 1.5f + a * x * b + x * b * a * 12;
    }}
}}
";
		var diagnostics = new[]
		{
			ExpectDiagnostic().WithLocation(11, 22),
			ExpectDiagnostic().WithLocation(11, 41),
			ExpectDiagnostic().WithLocation(11, 53)
		};

		await VerifyCSharpDiagnosticAsync(test, diagnostics);

		string fixedTest = $@"
using UnityEngine;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {typeof(T).Name}.zero;
        var a = 12;
        var b = 42;

        var result = 1.5f * a * b * x + a * b * x + 12 * a * b * x;
    }}
}}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task OnlyMultiplyExpression()
	{
		string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update() {
        float curAngle = 0, preAngle = 0, rev = 0;
        Transform target = transform;
        Vector3 offsetPerRound = Vector3.up;

        target.position += offsetPerRound * (curAngle - preAngle) / 360 * rev;
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task WhenMultiplyExpressionUsed()
	{
		string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update() {
        float curAngle = 0, preAngle = 0, rev = 0;
        Transform target = transform;
        Vector3 offsetPerRound = Vector3.up;

        target.position += offsetPerRound * (curAngle - preAngle) * 360 * rev;
    }
}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(11, 28);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update() {
        float curAngle = 0, preAngle = 0, rev = 0;
        Transform target = transform;
        Vector3 offsetPerRound = Vector3.up;

        target.position += (curAngle - preAngle) * 360 * rev * offsetPerRound;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
