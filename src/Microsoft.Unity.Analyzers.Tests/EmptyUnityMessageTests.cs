using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<EmptyUnityMessageAnalyzer, EmptyUnityMessageCodeFix>;

	public class EmptyUnityMessageTests
	{
		[Fact]
		public async Task EmptyFixedUpdate()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void FixedUpdate()
    {
    }

    private void Foo()
    {
    }
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(6, 18).WithArguments("FixedUpdate");

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{

    private void Foo()
    {
    }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task FixedUpdateWithBody()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void FixedUpdate()
    {
        Debug.Log(nameof(FixedUpdate));
    }

    private void Foo()
    {
    }
}
";
			await Verify.VerifyAnalyzerAsync(test);
		}
	}
}
