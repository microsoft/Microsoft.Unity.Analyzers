using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<UnityObjectNullHandlingAnalyzer, UnityObjectNullHandlingCodeFix>;

	public class UnityObjectNullHandlingTests : BaseTest<UnityObjectNullHandlingAnalyzer, UnityObjectNullHandlingCodeFix>
	{
		[Fact]
		public async Task FixIdentifierCoalescing()
		{
			const string test = @"
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

			var diagnostic = Verify.Diagnostic(UnityObjectNullHandlingAnalyzer.NullCoalescingRule).WithLocation(11, 10);

			const string fixedTest = @"
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
			const string test = @"
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

			var diagnostic = Verify.Diagnostic(UnityObjectNullHandlingAnalyzer.NullCoalescingRule).WithLocation(11, 10);

			const string fixedTest = @"
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
			const string test = @"
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

			var diagnostic = Verify.Diagnostic(UnityObjectNullHandlingAnalyzer.NullCoalescingRule).WithLocation(11, 10);

			const string fixedTest = @"
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

		[Fact]
		public async Task DetectNullPropagation()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform NP()
	{
		return transform?.transform;
	}
}
";

			var diagnostic = Verify.Diagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule).WithLocation(8, 10);

			await Verify.VerifyAnalyzerAsync(test, diagnostic);
		}
	}
}
