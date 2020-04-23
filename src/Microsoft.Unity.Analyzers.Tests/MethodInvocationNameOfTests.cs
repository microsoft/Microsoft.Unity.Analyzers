/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class MethodInvocationNameOfTests : BaseCodeFixVerifierTest<MethodInvocationAnalyzer, MethodInvocationNameOfCodeFix>
	{
		[Fact]
		public async Task TestInvoke()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Start()
    {
        Invoke(""InvokeMe"", 10.0f);
    }

    private void InvokeMe()
    {
    }
}";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 9)
				.WithArguments("InvokeMe");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Start()
    {
        Invoke(nameof(InvokeMe), 10.0f);
    }

    private void InvokeMe()
    {
    }
}";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task TestInvokeRepeating()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Start()
    {
        InvokeRepeating(""InvokeMe"", 10.0f, 10.0f);
    }

    private void InvokeMe()
    {
    }
}";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 9)
				.WithArguments("InvokeMe");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Start()
    {
        InvokeRepeating(nameof(InvokeMe), 10.0f, 10.0f);
    }

    private void InvokeMe()
    {
    }
}";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task TestInvokeNoMonoBehaviour()
		{
			const string test = @"
class Foo
{
    void Bar()
    {
        this.Invoke(""InvokeMe"", 10.0f);
    }

    private void Invoke(string name, object param) 
    {
    }

    private void InvokeMe()
    {
    }
}";

			await VerifyCSharpDiagnosticAsync(test);
		}

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
        StartCoroutine(nameof(InvokeMe));
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
        StopCoroutine(nameof(InvokeMe));
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
