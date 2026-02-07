/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class TagComparisonTests : BaseCodeFixVerifierTest<TagComparisonAnalyzer, TagComparisonCodeFix>
{
	[Fact]
	public async Task TagAsIdentifier()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(tag == ""tag1"");
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(CompareTag(""tag1""));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TagAsIdentifierTrivia()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        // comment
        Debug.Log(/* inner */ tag == ""tag1"" /* outer */);
        /* comment */
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(9, 31);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        // comment
        Debug.Log(/* inner */ CompareTag(""tag1"" /* outer */));
        /* comment */
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TagAsIdentifierInvoke()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(tag.Equals(""tag1""));
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(CompareTag(""tag1""));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TagAsIdentifierNeqInvoke()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(!tag.Equals(""tag1""));
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 20);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(!CompareTag(""tag1""));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TagAsIdentifierThisPrefix()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(this.tag == ""tag1"");
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(this.CompareTag(""tag1""));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TagAsIdentifierNeqNullRhs()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(null != tag);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(!CompareTag(null));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TagProperty()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(transform.tag == ""tag2"");
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(transform.CompareTag(""tag2""));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TagPropertyForGameObject()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(gameObject.tag == ""tag2"");
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(gameObject.CompareTag(""tag2""));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TagAsIdentifierRhs()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(""tag3"" == tag);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(CompareTag(""tag3""));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TagAsIdentifierRhsInvoke()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(""tag3"".Equals(tag));
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(CompareTag(""tag3""));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TagPropertyRhs()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(""tag4"" == transform.tag);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(transform.CompareTag(""tag4""));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task TagPropertyNeqNullRhs()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(null != transform.tag);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(!transform.CompareTag(null));
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task ObjectEqualsExplicit()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        if (object.Equals(transform.tag, null))
            return;
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 13);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        if (transform.CompareTag(null))
            return;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task ObjectEqualsImplicit()
	{
		const string test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        if (Equals(null, transform.tag))
            return;
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 13);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        if (transform.CompareTag(null))
            return;
    }
}
";
		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
