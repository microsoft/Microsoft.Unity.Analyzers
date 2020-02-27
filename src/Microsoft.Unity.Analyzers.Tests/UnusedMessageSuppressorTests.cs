/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

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
	}
}
