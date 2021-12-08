/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class MessageSignatureTests : BaseCodeFixVerifierTest<MessageSignatureAnalyzer, MessageSignatureCodeFix>
{
	[Fact]
	public async Task MessageSignature()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnApplicationPause(int foo, bool pause, string[] bar)
    {
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 18)
			.WithArguments("OnApplicationPause");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnApplicationPause(bool pause)
    {
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task MessageSignatureUnityLogic()
	{
		// Unity allows to specify less parameters if you don't need them
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnAnimatorIK()
    {
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task MessageSignatureUnityLogicBadType()
	{
		// But we enforce proper type
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnAnimatorIK(string bad)
    {
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 18)
			.WithArguments("OnAnimatorIK");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnAnimatorIK(int layerIndex)
    {
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task MessageSignatureUnityLogicExtraParameters()
	{
		// And we prevent extra parameters
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnAnimatorIK(int layerIndex, int extra)
    {
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 18)
			.WithArguments("OnAnimatorIK");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnAnimatorIK(int layerIndex)
    {
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task MessageSignatureWithInheritance()
	{
		// two declarations for OnDestroy (one in EditorWindow and one in ScriptableObject) 
		const string test = @"
using UnityEditor;

class TestWindow : EditorWindow
{
    private void OnDestroy(int foo)
    {
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 18)
			.WithArguments("OnDestroy");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEditor;

class TestWindow : EditorWindow
{
    private void OnDestroy()
    {
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
