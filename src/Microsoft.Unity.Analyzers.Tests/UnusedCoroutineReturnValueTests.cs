/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class UnusedCoroutineReturnValueTests : BaseTestCodeFixVerifier<UnusedCoroutineReturnValueAnalyzer, UnusedCoroutineReturnValueCodeFix>
	{
		[Fact]
		public void UnusedCoroutineReturnValueTest()
		{
			const string test = @"
using System.Collections;
using UnityEngine;

public class UnusedCoroutineScript : MonoBehaviour
{
    void Start()
    {
        UnusedCoroutine(2.0f);
    }

    private IEnumerator UnusedCoroutine(float waitTime)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(waitTime);
        }
    }
}
";

			var diagnostic = ExpectDiagnostic(UnusedCoroutineReturnValueAnalyzer.Id)
				.WithLocation(9, 9)
				.WithArguments("UnusedCoroutine");

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
using System.Collections;
using UnityEngine;

public class UnusedCoroutineScript : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(UnusedCoroutine(2.0f));
    }

    private IEnumerator UnusedCoroutine(float waitTime)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(waitTime);
        }
    }
}
";
			VerifyCSharpFix(test, fixedTest);
		}
	}
}
