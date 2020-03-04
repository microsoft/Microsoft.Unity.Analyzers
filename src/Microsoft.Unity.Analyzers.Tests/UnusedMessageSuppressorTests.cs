/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class UnusedMessageSuppressorTests : BaseSuppressorVerifierTest<UnusedMessageSuppressor>
	{
		[Fact]
		public void UnusedMethodSuppressed()
		{
			const string test = @"
using UnityEngine;

public class TestScript : MonoBehaviour
{
    private void Start()
    {
    }
}
";

			var suppressor = ExpectSuppressor(UnusedMessageSuppressor.MethodRule)
				.WithLocation(6, 18);

			VerifyCSharpDiagnostic(test, suppressor);
		}

		[Fact]
		public void UnusedParameterSuppressed()
		{
			const string test = @"
using UnityEngine;

public class TestScript : MonoBehaviour
{
    private void OnAnimatorIK(int layerIndex)
    {
        OnAnimatorIK(0);
    }
}
";

			var suppressor = ExpectSuppressor(UnusedMessageSuppressor.ParameterRule)
				.WithSuppressedDiagnosticMock(SyntaxKind.Parameter) // Use a mock while IDE analyzers have strong dependencies on Visual Studio components
				.WithLocation(6, 35);

			VerifyCSharpDiagnostic(test, suppressor);
		}
	}
}
