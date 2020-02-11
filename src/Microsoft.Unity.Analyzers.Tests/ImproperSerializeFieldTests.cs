using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<ImproperSerializeFieldAnalyzer, ImproperSerializeFieldCodeFix>;

	public class ImproperSerializeFieldTests
	{
		[Fact]
		public async Task Test ()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
}
";

			var diagnostic = Verify.Diagnostic();

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}
	}
}
