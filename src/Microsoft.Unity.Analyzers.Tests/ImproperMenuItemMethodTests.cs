/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class ImproperMenuItemMethodTests : BaseCodeFixVerifierTest<ImproperMenuItemMethodAnalyzer, ImproperMenuItemMethodCodeFix>
{
	[Fact]
	public async Task MissingStaticDeclaration()
	{
		const string test = @"
using UnityEngine;
using UnityEditor;

class Camera : MonoBehaviour
{
    [MenuItem(""Name"")]
    private void Menu1()
    {
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(7, 5);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;
using UnityEditor;

class Camera : MonoBehaviour
{
    [MenuItem(""Name"")]
    private static void Menu1()
    {
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task MissingStaticDeclarationComments()
	{
		const string test = @"
using UnityEngine;
using UnityEditor;

class Camera : MonoBehaviour
{
    // comment
    [MenuItem(""Name"")]
    private void Menu1() /* comment */
    {
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(8, 5);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;
using UnityEditor;

class Camera : MonoBehaviour
{
    // comment
    [MenuItem(""Name"")]
    private static void Menu1() /* comment */
    {
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task PreserveMenuCommand()
	{
		const string test = @"
using UnityEngine;
using UnityEditor;

class Camera : MonoBehaviour
{
    [MenuItem(""Name"")]
    private void Menu1(MenuCommand command)
    {
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(7, 5);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;
using UnityEditor;

class Camera : MonoBehaviour
{
    [MenuItem(""Name"")]
    private static void Menu1(MenuCommand command)
    {
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
