using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<InitializeOnLoadStaticCtorAnalyzer, InitializeOnLoadStaticCtorCodeFix>;

	public class InitializeOnLoadStaticCtorTests
	{
		[Fact]
		public async Task InitializeOnLoadWithoutStaticCtor()
		{
			const string test = @"
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
class Camera : MonoBehaviour
{
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(6, 7).WithArguments("Camera");

			const string fixedTest = @"
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
class Camera : MonoBehaviour
{
    static Camera()
    {
    }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task InitializeOnLoadWithImplicitStaticCtor()
		{
			const string test = @"
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public sealed class Camera : MonoBehaviour
{
    public static readonly int willGenerateImplicitStaticCtor = 666;
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(6, 21).WithArguments("Camera");

			const string fixedTest = @"
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public sealed class Camera : MonoBehaviour
{
    static Camera()
    {
    }

    public static readonly int willGenerateImplicitStaticCtor = 666;
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

	}
}
