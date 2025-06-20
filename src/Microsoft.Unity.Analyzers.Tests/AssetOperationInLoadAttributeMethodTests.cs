/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class AssetOperationInLoadAttributeMethodTests : BaseDiagnosticVerifierTest<AssetOperationInLoadAttributeMethodAnalyzer>
{
	[Fact]
	public async Task TestValidMethodUsage()
	{
		const string test = @"
using UnityEditor;

class Loader : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        AssetDatabase.LoadAllAssetsAtPath(""foo"");        
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task TestValidStaticCtorUsage()
	{
		const string test = @"
using UnityEditor;

class Loader
{
    static Loader() {
        AssetDatabase.LoadAllAssetsAtPath(""foo"");
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task TestMethodAttribute()
	{
		const string test = @"
using UnityEditor;

class Loader
{
    [InitializeOnLoadMethod]
    public void Foo() {
        AssetDatabase.LoadAllAssetsAtPath(""foo"");
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task TestStaticCtorAttribute()
	{
		const string test = @"
using UnityEditor;

[InitializeOnLoad]
class Loader
{
    static Loader() {
        AssetDatabase.LoadAllAssetsAtPath(""foo"");
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task TestIgnoreList()
	{
		const string test = @"
using UnityEditor;

class Loader
{
    [InitializeOnLoadMethod]
    public void Foo() {
        if (AssetDatabase.IsAssetImportWorkerProcess())
            return;
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

}
