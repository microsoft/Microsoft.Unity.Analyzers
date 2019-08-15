using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<UnityObjectNullCoalescingAnalyzer, UnityObjectNullCoalescingCodeFix>;

	public class UnityObjectNullCoalescingTests
	{
		[Fact]
		public async Task FixIdentifierCoalescing()
		{
			var test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform a;
	public Transform b;

	public Transform NC()
	{
		return a ?? b;
	}
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(11, 10);

			var fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform a;
	public Transform b;

	public Transform NC()
	{
		return a != null ? a : b;
	}
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task FixMemberCoalescing()
		{
			var test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform a;
	public Transform b;

	public Transform NC()
	{
		return this.a ?? this.b;
	}
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(11, 10);

			var fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform a;
	public Transform b;

	public Transform NC()
	{
		return this.a != null ? this.a : this.b;
	}
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}


		[Fact]
		public async Task CantFixSideEffect()
		{
			var test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform A() { return null; }
	public Transform B() { return null; }

	public Transform NC()
	{
		return A() ?? B();
	}
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(11, 10);

			var fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform A() { return null; }
	public Transform B() { return null; }

	public Transform NC()
	{
		return A() ?? B();
	}
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}
	}
}
