/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class UnityObjectNullHandlingSuppressorTests : BaseSuppressorVerifierTest<UnityObjectNullHandlingSuppressor>
	{
		[Fact]
		public async Task NullCoalescingSuppressedAsync()
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
				.WithSuppressedDiagnosticMock(SyntaxKind.ConditionalExpression) // Use a mock while IDE analyzers have strong dependencies on Visual Studio components
				.WithLocation(11, 16);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task NullPropagationSuppressedAsync()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public Transform NP()
    {
        return transform != null ? transform : null;
    }
}";

			var suppressor = ExpectSuppressor(UnityObjectNullHandlingSuppressor.NullPropagationRule)
				.WithSuppressedDiagnosticMock(SyntaxKind.ConditionalExpression) // Use a mock while IDE analyzers have strong dependencies on Visual Studio components
				.WithLocation(8, 16);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

	}
}
