/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class IndirectionMessageTests : BaseCodeFixVerifierTest<IndirectionMessageAnalyzer, IndirectionMessageCodeFix>
{
	[Fact]
	public async Task RemoveGameObjectProperty()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        GameObject original = null;
        GameObject duplicate = original.gameObject;
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(9, 32);
		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        GameObject original = null;
        GameObject duplicate = original;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task RemoveGameObjectPropertyArgumentOrParenthesis()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Method(GameObject argument)
    {
        GameObject original = null;
        Method(((original.gameObject)));
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(9, 18);
		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Method(GameObject argument)
    {
        GameObject original = null;
        Method(((original)));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task RemoveGameObjectPropertyComments()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        GameObject original = null;
        // comment
        GameObject duplicate = /* inner */ original.gameObject;
        /* comment */
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(10, 44);
		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        GameObject original = null;
        // comment
        GameObject duplicate = /* inner */ original;
        /* comment */
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task RemoveGameObjectPropertyMultiple()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        GameObject original = null;
        GameObject duplicate = original.gameObject.gameObject;
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(9, 32);
		await VerifyCSharpDiagnosticAsync(test, diagnostic, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        GameObject original = null;
        GameObject duplicate = original;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task RemoveGameObjectPropertyMethod()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        GameObject duplicate = GetGameObject().gameObject;
    }

    GameObject GetGameObject()
    {
        return null;
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(8, 32);
		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        GameObject duplicate = GetGameObject();
    }

    GameObject GetGameObject()
    {
        return null;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
