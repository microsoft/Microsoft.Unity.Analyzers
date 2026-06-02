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
	public async Task FixIdentifierCoalescingTrivia()
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
	public async Task FixNullPropagationTrivia()
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

		var context = AnalyzerVerificationContext
			.Default                       // see https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Scripting/UnityEngineObject.bindings.cs
			.WithAnalyzerFilter("CS0618"); // ignore Unity 2023.x warning CS0618 for now: 'Object.FindObjectOfType<T>()' is obsolete'

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
			.WithLocation(8, 9);

		await VerifyCSharpDiagnosticAsync(context, test, diagnostic);

		// we cannot fix with side-effects
		await VerifyCSharpFixAsync(test, test);
	}

	[Fact]
	public async Task FixNullPropagationInvocationStatement()
	{
		// See https://github.com/microsoft/Microsoft.Unity.Analyzers/issues/468
		// `obj?.Method(args);` used as a statement is not a member binding so the codefix
		// previously returned the document unchanged, leaving the VS lightbulb as a no-op.
		const string test = @"
using UnityEngine;

class Rotate : MonoBehaviour { }

class Camera : MonoBehaviour
{
    void Update()
    {
        gameObject?.AddComponent(typeof(Rotate));
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
			.WithLocation(10, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Rotate : MonoBehaviour { }

class Camera : MonoBehaviour
{
    void Update()
    {
        if (gameObject != null)
            gameObject.AddComponent(typeof(Rotate));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task FixNullPropagationInvocationStatementTrivia()
	{
		const string test = @"
using UnityEngine;

class Rotate : MonoBehaviour { }

class Camera : MonoBehaviour
{
    void Update()
    {
        // before
        gameObject?.AddComponent(typeof(Rotate)); // trailing
        // after
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
			.WithLocation(11, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Rotate : MonoBehaviour { }

class Camera : MonoBehaviour
{
    void Update()
    {
        // before
        if (gameObject != null)
            gameObject.AddComponent(typeof(Rotate)); // trailing
        // after
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task FixNullPropagationInvocationExpressionTrivia()
	{
		const string test = @"
using UnityEngine;

class Rotate : MonoBehaviour { }

class Camera : MonoBehaviour
{
    Component NP()
    {
        return /* inner */ gameObject?.AddComponent(typeof(Rotate));
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
			.WithLocation(10, 28);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Rotate : MonoBehaviour { }

class Camera : MonoBehaviour
{
    Component NP()
    {
        return /* inner */ gameObject != null ? gameObject.AddComponent(typeof(Rotate)) : null;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task FixNullPropagationInvocationExpression()
	{
		const string test = @"
using UnityEngine;

class Rotate : MonoBehaviour { }

class Camera : MonoBehaviour
{
    Component NP()
    {
        return gameObject?.AddComponent(typeof(Rotate));
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
			.WithLocation(10, 16);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Rotate : MonoBehaviour { }

class Camera : MonoBehaviour
{
    Component NP()
    {
        return gameObject != null ? gameObject.AddComponent(typeof(Rotate)) : null;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task FixNullPropagationInvocationChain()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    GameObject NP()
    {
        return transform?.GetChild(0).gameObject;
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
    GameObject NP()
    {
        return transform != null ? transform.GetChild(0).gameObject : null;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task CantFixNullPropagationValueTypeResult()
	{
		// `transform?.gameObject.activeInHierarchy` yields `bool?` (Nullable<bool>). A naive ternary
		// rewrite would have branches `bool` and `null` which cannot be unified - we must skip rather
		// than emit non-compiling code.
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    bool NP()
    {
        return transform?.gameObject.activeInHierarchy ?? false;
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
			.WithLocation(8, 16);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		// we cannot safely fix without introducing a cast
		await VerifyCSharpFixAsync(test, test);
	}

	[Fact]
	public async Task FixNullPropagationDanglingElse()
	{
		// The original `obj?.Foo();` is the `then` branch of an outer if-else; without block-wrapping
		// the rewrite would bind the outer `else` to the generated inner `if`.
		const string test = @"
using UnityEngine;

class Rotate : MonoBehaviour { }

class Camera : MonoBehaviour
{
    void Update()
    {
        if (enabled)
            gameObject?.AddComponent(typeof(Rotate));
        else
            Debug.Log(""no"");
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
			.WithLocation(11, 13);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Rotate : MonoBehaviour { }

class Camera : MonoBehaviour
{
    void Update()
    {
        if (enabled)
        {
            if (gameObject != null)
                gameObject.AddComponent(typeof(Rotate));
        }
        else
            Debug.Log(""no"");
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task CantFixNullPropagationNestedReceiverSideEffect()
	{
		// `GetComponent<Camera>().gameObject` is syntactically a member access, but the receiver is
		// an invocation. Strengthened HasSideEffect must reject this so we don't duplicate the call.
		const string test = @"
using UnityEngine;

class Rotate : MonoBehaviour { }

class Camera : MonoBehaviour
{
    void Update()
    {
        GetComponent<Camera>().gameObject?.AddComponent(typeof(Rotate));
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.NullPropagationRule)
			.WithLocation(10, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		// we cannot fix because the receiver (.gameObject) hangs off an invocation with side effects
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
	public async Task FixCoalescingAssignmentTrivia()
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

	[Fact]
	public async Task IsNullPatternExpression()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;

    public void Update()
    {
        if (a is null) { }
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.IsPatternRule)
			.WithLocation(10, 13);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;

    public void Update()
    {
        if (a == null) { }
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task IsNotNullPatternExpression()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;

    public void Update()
    {
        if (a is not null) { }
    }
}
";

		var diagnostic = ExpectDiagnostic(UnityObjectNullHandlingAnalyzer.IsPatternRule)
			.WithLocation(10, 13);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;

    public void Update()
    {
        if (a != null) { }
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task IsRegularPatternExpression()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public object a = null;

    public void Update()
    {
        if (a is not null) { }
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}
}
