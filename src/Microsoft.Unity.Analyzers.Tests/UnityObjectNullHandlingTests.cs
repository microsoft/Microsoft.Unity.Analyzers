/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class UnityObjectNullHandlingTests : BaseCodeFixVerifierTest<UnityObjectNullHandlingAnalyzer, UnityObjectNullHandlingCodeFix>
	{
		[Fact]
		public void FixIdentifierCoalescing()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void FixMemberCoalescing()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}


		[Fact]
		public void CantFixSideEffect()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void DetectNullPropagation()
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

			VerifyCSharpDiagnostic(test, diagnostic);
		}
	}
}
