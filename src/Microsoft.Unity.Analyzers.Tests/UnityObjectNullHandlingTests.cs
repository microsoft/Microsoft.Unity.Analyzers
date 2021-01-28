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
		public async Task FixIdentifierCoalescing()
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
				.WithLocation(11, 16);

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
		public async Task FixIdentifierCoalescingComments()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    public Transform NC()
    {
        // comment
        return /* inner */ a ?? b /* outer */;
        /* comment */
    }
}
";

			var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullCoalescingRule)
				.WithLocation(12, 28);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    public Transform NC()
    {
        // comment
        return /* inner */ a != null ? a : b /* outer */;
        /* comment */
    }
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task FixMemberCoalescing()
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
				.WithLocation(11, 16);

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

			var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullCoalescingRule)
				.WithLocation(11, 16);

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

			var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
				.WithLocation(8, 16);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);
		}

		[Fact]
		public async Task FixIdentifierCoalescingAssignment()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    void Update()
    {
        a ??= b;
    }
}
";

			var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullCoalescingRule)
				.WithLocation(11, 9);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    void Update()
    {
        a = a != null ? a : b;
    }
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task FixIdentifierCoalescingAssignmentComments()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    void Update()
    {
        // comment
        /* inner */ a ??= b /* outer */;
        /* comment */
    }
}
";

			var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullCoalescingRule)
				.WithLocation(12,21);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    void Update()
    {
        // comment
        /* inner */ a = a != null ? a : b /* outer */;
        /* comment */
    }
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task FixMemberCoalescingAssignment()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    void Update()
    {
        this.a ??= this.b;
    }
}
";

			var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullCoalescingRule)
				.WithLocation(11, 9);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    void Update()
    {
        this.a = this.a != null ? this.a : this.b;
    }
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}
	}
}
