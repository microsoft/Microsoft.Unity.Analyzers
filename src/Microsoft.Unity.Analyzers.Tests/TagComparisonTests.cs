/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class TagComparisonTests : BaseCodeFixVerifierTest<TagComparisonAnalyzer, TagComparisonCodeFix>
	{
		[Fact]
		public async Task TagAsIdentifierAsync()
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
		public async Task TagAsIdentifierInvokeAsync()
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
		public async Task TagAsIdentifierNeqInvokeAsync()
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
		public async Task TagAsIdentifierThisPrefixAsync()
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
		public async Task TagAsIdentifierNeqNullRhsAsyncAsync()
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
		public async Task TagPropertyAsync()
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
		public async Task TagPropertyForGameObjectAsync()
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
		public async Task TagAsIdentifierRhsAsync()
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
		public async Task TagAsIdentifierRhsInvokeAsync()
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
		public async Task TagPropertyRhsAsync()
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
		public async Task TagPropertyNeqNullRhsAsync()
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
		public async Task ObjectEqualsExplicitAsync()
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
		public async Task ObjectEqualsImplicitAsync()
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
}
