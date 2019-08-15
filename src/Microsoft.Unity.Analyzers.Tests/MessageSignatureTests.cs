using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<MessageSignatureAnalyzer, MessageSignatureCodeFix>;

	public class MessageSignatureTests
	{
		[Fact]
		public async Task MessageSignature()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnApplicationPause(int foo, bool pause, string[] bar)
    {
    }
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(6, 5).WithArguments("OnApplicationPause");

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void OnApplicationPause(bool pause)
    {
    }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}
	}
}
