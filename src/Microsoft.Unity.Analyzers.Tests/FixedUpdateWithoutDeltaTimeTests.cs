using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<FixedUpdateWithoutDeltaTimeAnalyzer, FixedUpdateWithoutDeltaTimeCodeFix>;

	public class FixedUpdateWithoutDeltaTimeTests
	{
		[Fact]
		public async Task FixedUpdateWithDeltaTime()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
     public void FixedUpdate()
     {
         var foo = Time.deltaTime;
     }
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(8, 25);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
     public void FixedUpdate()
     {
         var foo = Time.fixedDeltaTime;
     }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}
	}
}
