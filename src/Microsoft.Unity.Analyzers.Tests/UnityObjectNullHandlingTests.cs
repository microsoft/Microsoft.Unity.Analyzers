/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class UnityObjectNullHandlingTests : BaseCodeFixVerifierTest<UnityObjectNullHandlingAnalyzer, UnityObjectNullHandlingCodeFix>
	{
		[Fact]
		public async Task FixIdentifierCoalescingAsync()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform a = null;
	public Transform b = null;

	public Transform NC()
	{
		return a ?? b;
	}
}
";

			var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullCoalescingRule)
				.WithLocation(11, 10);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform a = null;
	public Transform b = null;

	public Transform NC()
	{
		return a != null ? a : b;
	}
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task FixMemberCoalescingAsync()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform a = null;
	public Transform b = null;

	public Transform NC()
	{
		return this.a ?? this.b;
	}
}
";

			var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullCoalescingRule)
				.WithLocation(11, 10);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	public Transform a = null;
	public Transform b = null;

	public Transform NC()
	{
		return this.a != null ? this.a : this.b;
	}
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}


		[Fact]
		public async Task CantFixSideEffectAsync()
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

			var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullCoalescingRule)
				.WithLocation(11, 10);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

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
			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task DetectNullPropagationAsync()
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

			var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
				.WithLocation(8, 10);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);
		}
	}
}
