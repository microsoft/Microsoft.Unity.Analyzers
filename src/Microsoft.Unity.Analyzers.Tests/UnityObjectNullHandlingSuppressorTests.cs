/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class UnityObjectNullHandlingSuppressorTests : BaseSuppressorVerifierTest<UnityObjectNullHandlingSuppressor>
	{
		[Fact]
		public async Task NullCoalescingSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    public Transform NC()
    {
        return a != null ? a : b;
    }
}";

			var suppressor = ExpectSuppressor(UnityObjectNullHandlingSuppressor.NullCoalescingRule)
				.WithLocation(11, 16);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task NullCoalescingParenthesisSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    public Transform NC()
    {
        return (a != null) ? a : b;
    }
}";

			var suppressor = ExpectSuppressor(UnityObjectNullHandlingSuppressor.NullCoalescingRule)
				.WithLocation(11, 16);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task NullCoalescingMethodArgumentSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    public Transform Method(Transform o)
    {
        return o;
    }

    public Transform NC()
    {
        return Method((a != null) ? a : b);
    }
}";

			var suppressor = ExpectSuppressor(UnityObjectNullHandlingSuppressor.NullCoalescingRule)
				.WithLocation(16, 23);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task NullPropagationSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void NP(Transform o)
    {
        var v = o == null ? null : o.ToString();
    }
}";

			var suppressor = ExpectSuppressor(UnityObjectNullHandlingSuppressor.NullPropagationRule)
				.WithLocation(8, 17);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task NullPropagationParenthesisSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void NP(Transform o)
    {
        var v = ((o == null)) ? null : o.ToString();
    }
}";

			var suppressor = ExpectSuppressor(UnityObjectNullHandlingSuppressor.NullPropagationRule)
				.WithLocation(8, 17);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task NullPropagationMethodArgumentSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Method(object o) {
    }

    public void NP(Transform o)
    {
        Method(((o == null)) ? null : o.ToString());
    }
}";

			var suppressor = ExpectSuppressor(UnityObjectNullHandlingSuppressor.NullPropagationRule)
				.WithLocation(11, 16);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task NullCoalescingAssignmentSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    void Update()
    {
        a = a != null ? a : b;
    }
}";

			var suppressor = ExpectSuppressor(UnityObjectNullHandlingSuppressor.NullCoalescingRule)
				.WithLocation(11, 13);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task NullCoalescingAssignmentParenthesisSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    void Update()
    {
        a = (a != null) ? a : b;
    }
}";

			var suppressor = ExpectSuppressor(UnityObjectNullHandlingSuppressor.NullCoalescingRule)
				.WithLocation(11, 13);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task NullCoalescingAssignmentMethodArgumentSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform a = null;
    public Transform b = null;

    public Transform Method(Transform o)
    {
        return o;
    }

    void Update()
    {
        a = Method((a != null) ? a : b);
    }
}";

			var suppressor = ExpectSuppressor(UnityObjectNullHandlingSuppressor.NullCoalescingRule)
				.WithLocation(16, 20);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}
	}
}
