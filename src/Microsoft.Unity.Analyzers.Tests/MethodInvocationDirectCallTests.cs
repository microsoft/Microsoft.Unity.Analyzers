/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class MethodInvocationDirectCallTests : BaseCodeFixVerifierTest<MethodInvocationAnalyzer, MethodInvocationDirectCallCodeFix>
	{
		[Fact]
		public async Task TestStartCoroutine()
		{
			const string test = @"
using UnityEngine;
using System.Collections;

class Camera : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(""InvokeMe"");
    }

    private IEnumerator InvokeMe()
    {
		return null;
    }
}";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(9, 9)
				.WithArguments("InvokeMe");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;
using System.Collections;

class Camera : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(InvokeMe());
    }

    private IEnumerator InvokeMe()
    {
		return null;
    }
}";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task TestStartCoroutineComments()
		{
			const string test = @"
using UnityEngine;
using System.Collections;

class Camera : MonoBehaviour
{
    void Start()
    {
        // comment
        StartCoroutine(/* inner */ ""InvokeMe"" /* outer */);
        /* comment */
    }

    private IEnumerator InvokeMe()
    {
		return null;
    }
}";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(10, 9)
				.WithArguments("InvokeMe");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;
using System.Collections;

class Camera : MonoBehaviour
{
    void Start()
    {
        // comment
        StartCoroutine(/* inner */ InvokeMe() /* outer */);
        /* comment */
    }

    private IEnumerator InvokeMe()
    {
		return null;
    }
}";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task TestStopCoroutine()
		{
			const string test = @"
using UnityEngine;
using System.Collections;

class Camera : MonoBehaviour
{
    void Start()
    {
        StopCoroutine(""InvokeMe"");
    }

    private IEnumerator InvokeMe()
    {
		return null;
    }
}";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(9, 9)
				.WithArguments("InvokeMe");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;
using System.Collections;

class Camera : MonoBehaviour
{
    void Start()
    {
        StopCoroutine(InvokeMe());
    }

    private IEnumerator InvokeMe()
    {
		return null;
    }
}";

			await VerifyCSharpFixAsync(test, fixedTest);
		}
	}
}
