/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class TagComparisonTests : BaseCodeFixVerifierTest<TagComparisonAnalyzer, TagComparisonCodeFix>
	{
		[Fact]
		public void TagAsIdentifier()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void TagAsIdentifierInvoke()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void TagAsIdentifierNeqInvoke()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void TagAsIdentifierThisPrefix()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void TagAsIdentifierNeqNullRhs()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void TagProperty()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void TagPropertyForGameObject()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void TagAsIdentifierRhs()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void TagAsIdentifierRhsInvoke()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void TagPropertyRhs()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void TagPropertyNeqNullRhs()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void ObjectEqualsExplicit()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void ObjectEqualsImplicit()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

	}
}
