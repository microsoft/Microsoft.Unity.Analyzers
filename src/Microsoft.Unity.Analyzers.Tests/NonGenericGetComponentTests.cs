using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<NonGenericGetComponentAnalyzer, NonGenericGetComponentCodeFix>;

	public class NonGenericGetComponentTests
	{
		[Fact]
		public async Task GetComponentAs()
		{
			var test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent(typeof(Rigidbody)) as Rigidbody;
    }
}
";

			var diagnostic = Verify.Diagnostic()
				.WithLocation(10, 14)
				.WithArguments("GetComponent");

			var fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task CastGetComponent()
		{
			var test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	private Rigidbody rb;

    private void Start()
    {
        rb = (Rigidbody)GetComponent(typeof(Rigidbody));
    }
}
";

			var diagnostic = Verify.Diagnostic()
				.WithLocation(10, 25)
				.WithArguments("GetComponent");

			var fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task GetComponentsInChildrenBoolean()
		{
			var test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        GetComponentsInChildren(typeof(Rigidbody), true);
    }
}
";

			var diagnostic = Verify.Diagnostic()
				.WithLocation(8, 9)
				.WithArguments("GetComponentsInChildren");

			var fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        GetComponentsInChildren<Rigidbody>(true);
    }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task GetComponentTypeVariable()
		{
			var test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        var t = typeof(Rigidbody);
        GetComponent(t);
    }
}
";

			// We're assuming that using GetComponent with anything else than a typeof
			// argument is from a computation that makes it impossible to use the generic form
			await Verify.VerifyAnalyzerAsync(test);
		}

		[Fact]
		public async Task GetComponentGeneric()
		{
			var test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Rigidbody>();
    }
}
";

			// Verify we're not misreporting an already generic usage
			await Verify.VerifyAnalyzerAsync(test);
		}
	}
}
