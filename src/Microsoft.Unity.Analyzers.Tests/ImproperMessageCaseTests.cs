/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class ImproperMessageCaseTests : BaseCodeFixVerifierTest<ImproperMessageCaseAnalyzer, ImproperMessageCaseCodeFix>
{
	[Fact]
	public async Task ProperlyCasedUpdate()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Update()
    {
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task ImproperlyCasedUpdate()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void UPDATE()
    {
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 18)
			.WithArguments("UPDATE");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Update()
    {
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task ImproperlyCasedStaticUpdateIgnored()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private static void UPDATE()
    {
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}
	
	[Fact]
	public async Task ImproperlyCasedRealStaticMessage()
	{
		const string test = @"
using UnityEditor;

class App : AssetPostprocessor
{
    static bool OnPREGeneratingCSProjectFiles()
    {
        return false;
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 17)
			.WithArguments("OnPREGeneratingCSProjectFiles");

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
	public async Task ProperlyCasedOnPostprocessAllAssets()
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
	public async Task ProperlyCasedOnPostprocessAllAssetsOverload()
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
	public async Task ImproperlyCasedPostprocessAllAssets()
	{
		const string test = @"
using UnityEditor;

class App : AssetPostprocessor
{
    static void OnPostPROCESSAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 17)
			.WithArguments("OnPostPROCESSAllAssets");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEditor;

class App : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task ImproperlyCasedOnPostprocessAllAssetsOverload()
	{
		const string test = @"
using UnityEditor;

class App : AssetPostprocessor
{
    static void OnPostProcessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 17)
			.WithArguments("OnPostProcessAllAssets");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEditor;

class App : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

}
