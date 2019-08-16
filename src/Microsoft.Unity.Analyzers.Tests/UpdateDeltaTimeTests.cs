using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<UpdateDeltaTimeAnalyzer, UpdateDeltaTimeCodeFix>;

	public class UpdateWithoutDeltaTimeTests
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

			var diagnostic = Verify.Diagnostic(UpdateDeltaTimeAnalyzer.FixedUpdateId).WithLocation(8, 25);

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

		[Fact]
		public async Task UpdateWithFixedDeltaTime()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
     public void Update()
     {
         var foo = Time.fixedDeltaTime;
     }
}
";

			var diagnostic = Verify.Diagnostic(UpdateDeltaTimeAnalyzer.UpdateId).WithLocation(8, 25);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
     public void Update()
     {
         var foo = Time.deltaTime;
     }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}
	}
}
