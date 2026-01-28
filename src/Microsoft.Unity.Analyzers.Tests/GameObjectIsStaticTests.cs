/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class GameObjectIsStaticTests : BaseDiagnosticVerifierTest<GameObjectIsStaticAnalyzer>
{
	[Fact]
	public async Task IsStaticInUpdateMethod()
	{
		const string test = @"
using UnityEngine;

class TestScript : MonoBehaviour
{
    void Update()
    {
        var go = new GameObject();
        if (go.isStatic)
        {
            Debug.Log(""Static"");
        }
    }
}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(9, 16);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task IsStaticInOnValidateMethod_NoDiagnostic()
	{
		const string test = @"
using UnityEngine;

class TestScript : MonoBehaviour
{
    void OnValidate()
    {
        var go = new GameObject();
        if (go.isStatic)
        {
            Debug.Log(""Static"");
        }
    }
}
";
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task IsStaticInsideUnityEditorDirective_NoDiagnostic()
	{
		const string test = @"
using UnityEngine;

class TestScript : MonoBehaviour
{
    void Update()
    {
        var go = new GameObject();
#if UNITY_EDITOR
        if (go.isStatic)
        {
            Debug.Log(""Static"");
        }
#endif
    }
}
";
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task IsStaticOutsideUnityEditorDirective()
	{
		const string test = @"
using UnityEngine;

class TestScript : MonoBehaviour
{
    void Update()
    {
        var go = new GameObject();
#if UNITY_EDITOR
        Debug.Log(""In Editor"");
#endif
        if (go.isStatic)
        {
            Debug.Log(""Static"");
        }
    }
}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(12, 16);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task IsStaticInNonUnityClass()
	{
		const string test = @"
using UnityEngine;

class TestScript
{
    void DoSomething()
    {
        var go = new GameObject();
        if (go.isStatic)
        {
        }
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(9, 16);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task IsStaticInScriptableObjectOnValidate_NoDiagnostic()
	{
		const string test = @"
using UnityEngine;

class TestScriptableObject : ScriptableObject
{
    void OnValidate()
    {
        var go = new GameObject();
        if (go.isStatic)
        {
        }
    }
}
";
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task IsStaticWithNestedUnityEditorDirective_NoDiagnostic()
	{
		const string test = @"
using UnityEngine;

class TestScript : MonoBehaviour
{
    void Update()
    {
        var go = new GameObject();
#if UNITY_EDITOR
#if DEBUG
        if (go.isStatic)
        {
        }
#endif
#endif
    }
}
";
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task IsStaticWithComplexDirective_NoDiagnostic()
	{
		const string test = @"
using UnityEngine;

class TestScript : MonoBehaviour
{
    void Update()
    {
        var go = new GameObject();
#if UNITY_EDITOR || DEBUG
        if (go.isStatic)
        {
        }
#endif
    }
}
";
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task IsStaticInsideNegatedUnityEditorDirective()
	{
		const string test = @"
using UnityEngine;

class TestScript : MonoBehaviour
{
    void Update()
    {
        var go = new GameObject();
#if !UNITY_EDITOR
        if (go.isStatic)
        {
            Debug.Log(""Static"");
        }
#endif
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 16);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}
}
