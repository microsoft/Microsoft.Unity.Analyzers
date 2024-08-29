/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class Vector2MathTests : VectorMathTests<Vector2>;
public class Vector3MathTests : VectorMathTests<Vector3>;
public class Vector4MathTests : VectorMathTests<Vector4>;
public class Float2MathTests : VectorMathTests<float2>;
public class Float3MathTests : VectorMathTests<float3>;
public class Float4MathTests : VectorMathTests<float4>;

public abstract class VectorMathTests<T> : BaseCodeFixVerifierTest<VectorMathAnalyzer, VectorMathCodeFix>
{
	private readonly AnalyzerVerificationContext _context = AnalyzerVerificationContext
		.Default
		.WithAnalyzerFilter("CS8019"); // Unnecessary using directive

	private static readonly string _typeName = typeof(T).Name;

	[Fact]
	public async Task AlreadySorted()
	{
		string test = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {_typeName}.zero;
        var a = 12;
        var b = 42;

        var result = a * b * x;
    }}
}}
";

		await VerifyCSharpDiagnosticAsync(_context, test);
	}

	[Fact]
	public async Task SimpleOrdering()
	{
		string test = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {_typeName}.zero;
        var a = 12;
        var b = 42;

        var result = b * x * a;
    }}
}}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(12, 22);

		await VerifyCSharpDiagnosticAsync(_context, test, diagnostic);

		string fixedTest = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {_typeName}.zero;
        var a = 12;
        var b = 42;

        var result = a * b * x;
    }}
}}
";
		await VerifyCSharpFixAsync(_context, test, fixedTest);
	}

	[Fact]
	public async Task SimpleOrderingTrivia()
	{
		string test = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {_typeName}.zero;
        var a = 12;
        var b = 42;

        var result = /* left */ b * x * a /* right */;
    }}
}}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(12, 33);

		await VerifyCSharpDiagnosticAsync(_context, test, diagnostic);

		string fixedTest = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {_typeName}.zero;
        var a = 12;
        var b = 42;

        var result = /* left */ a * b * x /* right */;
    }}
}}
";
		await VerifyCSharpFixAsync(_context, test, fixedTest);
	}

	[Fact]
	public async Task MethodArgument()
	{
		string test = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {_typeName}.zero;
        var a = 12;
        var b = 42;

        {_typeName}.zero.Equals(b * x * a);
    }}
}}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(12, 22 + _typeName.Length);

		await VerifyCSharpDiagnosticAsync(_context, test, diagnostic);

		string fixedTest = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {_typeName}.zero;
        var a = 12;
        var b = 42;

        {_typeName}.zero.Equals(a * b * x);
    }}
}}
";
		await VerifyCSharpFixAsync(_context, test, fixedTest);
	}

	[Fact]
	public async Task Parenthesis()
	{
		string test = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {_typeName}.zero;
        var a = 12;
        var b = 42;

        var result = x + (b * x * a);
    }}
}}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(12, 27);

		await VerifyCSharpDiagnosticAsync(_context, test, diagnostic);

		string fixedTest = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {_typeName}.zero;
        var a = 12;
        var b = 42;

        var result = x + (a * b * x);
    }}
}}
";
		await VerifyCSharpFixAsync(_context, test, fixedTest);
	}

	[Fact]
	public async Task Multiple()
	{
		string test = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {_typeName}.zero;
        var a = 12;
        var b = 42;

        var result = b * x * a * 1.5f + a * x * b + x * b * a * 12;
    }}
}}
";
		var diagnostics = new[]
		{
			ExpectDiagnostic().WithLocation(12, 22),
			ExpectDiagnostic().WithLocation(12, 41),
			ExpectDiagnostic().WithLocation(12, 53)
		};

		await VerifyCSharpDiagnosticAsync(_context, test, diagnostics);

		string fixedTest = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        var x = {_typeName}.zero;
        var a = 12;
        var b = 42;

        var result = 1.5f * a * b * x + a * b * x + 12 * a * b * x;
    }}
}}
";
		await VerifyCSharpFixAsync(_context, test, fixedTest);
	}

	[Fact]
	public async Task OnlyMultiplyExpression()
	{
		string test = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        float curAngle = 0, preAngle = 0, rev = 0;
        {_typeName} offsetPerRound = {_typeName}.zero;
        {_typeName} result = offsetPerRound * (curAngle - preAngle) / 360 * rev;
    }}
}}
";

		await VerifyCSharpDiagnosticAsync(_context, test);
	}

	[Fact]
	public async Task WhenMultiplyExpressionUsed()
	{
		string test = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        float curAngle = 0, preAngle = 0, rev = 0;
        {_typeName} offsetPerRound = {_typeName}.zero;
        {_typeName} result = offsetPerRound * (curAngle - preAngle) * 360 * rev;
    }}
}}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 19 + _typeName.Length);

		await VerifyCSharpDiagnosticAsync(_context, test, diagnostic);

		string fixedTest = $@"
using UnityEngine;
using Unity.Mathematics;

class Camera : MonoBehaviour
{{
    void Update() {{
        float curAngle = 0, preAngle = 0, rev = 0;
        {_typeName} offsetPerRound = {_typeName}.zero;
        {_typeName} result = (curAngle - preAngle) * 360 * rev * offsetPerRound;
    }}
}}
";
		await VerifyCSharpFixAsync(_context, test, fixedTest);
	}
}
