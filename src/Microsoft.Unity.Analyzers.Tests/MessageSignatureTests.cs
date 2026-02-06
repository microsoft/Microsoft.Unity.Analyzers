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
	public async Task IgnoreStaticMessageSignature()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public static void Start(int foo)
    {
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task StaticMessageSignature()
	{
		const string test = @"
using UnityEditor;

class App : AssetPostprocessor
{
    static bool OnPreGeneratingCSProjectFiles(int foo)
    {
        return false;
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 17)
			.WithArguments("OnPreGeneratingCSProjectFiles");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEditor;

class App : AssetPostprocessor
{
    static bool OnPreGeneratingCSProjectFiles()
    {
        return false;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task IgnoreInstanceMessageSignature()
	{
		const string test = @"
using UnityEditor;

class App : AssetPostprocessor
{
    bool OnPreGeneratingCSProjectFiles(int foo)
    {
        return false;
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task MessageSignatureUnityLogic()
	{
		// Unity allows to specify fewer parameters if you don't need them
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

	[Fact]
	public async Task MessageSignatureOverload()
	{
		const string test = @"
using UnityEditor;

class App : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task MessageSignatureDidDomainReloadOverload()
	{
		const string test = @"
using UnityEditor;

class App : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task AwaitableMessageSignature()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private async Awaitable Start()
    {
        await Awaitable.WaitForSecondsAsync(1);
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task AwaitableMessageSignatureFixed()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private async Awaitable Start(int foo)
    {
        await Awaitable.WaitForSecondsAsync(1);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 29)
			.WithArguments("Start");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private async Awaitable Start()
    {
        await Awaitable.WaitForSecondsAsync(1);
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}



	[Fact]
	public async Task AwaitableOfMessageSignature()
	{
		const string test = @"
using UnityEngine;
using UnityEditor;

class App : AssetPostprocessor
{
    static async Awaitable<bool> OnPreGeneratingCSProjectFiles()
    {
        await Awaitable.WaitForSecondsAsync(1);
        return false;
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task AwaitableOfMessageSignatureFixed()
	{
		const string test = @"
using UnityEngine;
using UnityEditor;

class App : AssetPostprocessor
{
    static async Awaitable<bool> OnPreGeneratingCSProjectFiles(int foo)
    {
        await Awaitable.WaitForSecondsAsync(1);
        return false;
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(7, 34)
			.WithArguments("OnPreGeneratingCSProjectFiles");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;
using UnityEditor;

class App : AssetPostprocessor
{
    static async Awaitable<bool> OnPreGeneratingCSProjectFiles()
    {
        await Awaitable.WaitForSecondsAsync(1);
        return false;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task BadMessageSignatureWithUsedMethod()
	{
		const string test = @"
using UnityEditor;

class Camera : Editor
{
    private void Foo()
    {
        OnSceneGUI(null);
    }

    private void OnSceneGUI(object foo)
    {
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(11, 18)
			.WithArguments("OnSceneGUI");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		// In this special case, we do not provide a codefix, given it would break the code as the message is -wrongly- used elsewhere
		await VerifyCSharpFixAsync(test, test);
	}

	[Fact]
	public async Task MessageSignatureTrivia()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    // leading comment
    private void OnApplicationPause(int foo, bool pause, string[] bar) // trailing comment
    {
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(7, 18)
			.WithArguments("OnApplicationPause");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    // leading comment
    private void OnApplicationPause(bool pause) // trailing comment
    {
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
