using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityAnalyzerVerifier<UnityObjectNullPropagationAnalyzer>;

	public class UnityObjectNullPropagationTests
	{
		[Fact]
		public async Task DetectNullPropagation()
		{
			var test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform NP()
	{
		return transform?.transform;
	}
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(8, 10);

			await Verify.VerifyAnalyzerAsync(test, diagnostic);
		}
	}
}
