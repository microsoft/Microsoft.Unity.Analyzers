/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

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
	public async Task CantFixCoalescingSideEffect()
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

		// we cannot fix with side-effects
		await VerifyCSharpFixAsync(test, test);
	}

	[Fact]
	public async Task FixNullPropagation()
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

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform NP()
    {
        return transform != null ? transform.transform : null;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task FixNullPropagationComments()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform NP()
    {
        // comment
        return /* inner */ transform?.transform;
        /* comment */
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
			.WithLocation(9, 28);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform NP()
    {
        // comment
        return /* inner */ transform != null ? transform.transform : null;
        /* comment */
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task CantFixNullPropagationSideEffect()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform NP()
    {
        FindObjectOfType<GameObject>()?.gameObject.SetActive(false);
        return null;;
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
			.WithLocation(8, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		// we cannot fix with side-effects
		await VerifyCSharpFixAsync(test, test);
	}


	[Fact]
	public async Task FixCoalescingAssignment()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    public Transform NP()
    {
        return a ??= b;
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.CoalescingAssignmentRule)
			.WithLocation(11, 16);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    public Transform NP()
    {
        return a = a != null ? a : b;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task FixCoalescingAssignmentComments()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    public Transform NP()
    {
        // comment
        return /* inner */ a ??= b /* outer */;
        /* comment */
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.CoalescingAssignmentRule)
			.WithLocation(12, 28);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    public Transform NP()
    {
        // comment
        return /* inner */ a = a != null ? a : b /* outer */;
        /* comment */
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}


	[Fact]
	public async Task CoalescingAssignmentForRegularObjects()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public System.Object a = null;
    public System.Object b = null;

    public System.Object NP()
    {
        return a ??= b;
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}
}
