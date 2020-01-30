using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<UnusedCoroutineReturnValueAnalyzer, UnusedCoroutineReturnValueCodeFix>;

	public class UnusedCoroutineReturnValueTests
	{
		[Fact]
		public async Task UnusedCoroutineReturnValueTest()
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

			var diagnostic = Verify.Diagnostic(UnusedCoroutineReturnValueAnalyzer.Id).WithLocation(9, 9).WithArguments("UnusedCoroutine");

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
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}
	}
}
