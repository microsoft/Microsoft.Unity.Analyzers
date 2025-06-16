/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class CacheYieldInstructionAnalyzerTests : BaseCodeFixVerifierTest<CacheYieldInstructionAnalyzerAnalyzer, CacheYieldInstructionAnalyzerCodeFix>
{
	[Fact]
	public async Task TestWaitForSecondsStaticAsync()
	{
		const string test = @"
using System.Collections;
using UnityEngine;

class Camera : MonoBehaviour
{
    static IEnumerator Coroutine()
    {
        yield return new WaitForSeconds(1.666f);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(9, 22);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using System.Collections;
using UnityEngine;

class Camera : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds1_666 = new WaitForSeconds(1.666f);

    static IEnumerator Coroutine()
    {
        yield return _waitForSeconds1_666;
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TestWaitForSecondsOnlyLiteralAsync()
	{
		const string test = @"
using System.Collections;
using UnityEngine;

class Camera : MonoBehaviour
{
    IEnumerator Coroutine()
    {
        float a = 1f;
        yield return new WaitForSeconds(a);
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task TestWaitForSecondsMultipleAsync()
	{
		const string test = @"
using System.Collections;
using UnityEngine;

class Camera : MonoBehaviour
{
    IEnumerator Coroutine1()
    {
        yield return new WaitForSeconds(1f);
    }

    IEnumerator Coroutine2()
    {
        yield return new WaitForSeconds(2f);
    }
}
";

		var diagnostic1 = ExpectDiagnostic()
			.WithLocation(9, 22);

		var diagnostic2 = ExpectDiagnostic()
			.WithLocation(14, 22);

		await VerifyCSharpDiagnosticAsync(test, diagnostic1, diagnostic2);

		const string fixedTest = @"
using System.Collections;
using UnityEngine;

class Camera : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds2 = new WaitForSeconds(2f);
    private static WaitForSeconds _waitForSeconds1 = new WaitForSeconds(1f);

    IEnumerator Coroutine1()
    {
        yield return _waitForSeconds1;
    }

    IEnumerator Coroutine2()
    {
        yield return _waitForSeconds2;
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TestWaitForSecondsReuseAsync()
	{
		const string test = @"
using System.Collections;
using UnityEngine;

class Camera : MonoBehaviour
{
    IEnumerator Coroutine1()
    {
        yield return new WaitForSeconds(1f);
    }

    IEnumerator Coroutine2()
    {
        yield return new WaitForSeconds(1f);
    }
}
";

		var diagnostic1 = ExpectDiagnostic()
			.WithLocation(9, 22);

		var diagnostic2 = ExpectDiagnostic()
			.WithLocation(14, 22);

		await VerifyCSharpDiagnosticAsync(test, diagnostic1, diagnostic2);

		const string fixedTest = @"
using System.Collections;
using UnityEngine;

class Camera : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds1 = new WaitForSeconds(1f);

    IEnumerator Coroutine1()
    {
        yield return _waitForSeconds1;
    }

    IEnumerator Coroutine2()
    {
        yield return _waitForSeconds1;
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TestWaitMultipleAsync()
	{
		const string test = @"
using System.Collections;
using UnityEngine;

class Camera : MonoBehaviour
{
    IEnumerator Coroutine1()
    {
        yield return new WaitForSeconds(1f);
    }

    IEnumerator Coroutine2()
    {
        yield return new WaitForSecondsRealtime(1f);
    }
}
";

		var diagnostic1 = ExpectDiagnostic()
			.WithLocation(9, 22);

		var diagnostic2 = ExpectDiagnostic()
			.WithLocation(14, 22);

		await VerifyCSharpDiagnosticAsync(test, diagnostic1, diagnostic2);

		const string fixedTest = @"
using System.Collections;
using UnityEngine;

class Camera : MonoBehaviour
{
    private static WaitForSecondsRealtime _waitForSecondsRealtime1 = new WaitForSecondsRealtime(1f);
    private static WaitForSeconds _waitForSeconds1 = new WaitForSeconds(1f);

    IEnumerator Coroutine1()
    {
        yield return _waitForSeconds1;
    }

    IEnumerator Coroutine2()
    {
        yield return _waitForSecondsRealtime1;
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
