/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class UnusedMessageSuppressorTests : BaseSuppressorVerifierTest<UnusedMessageSuppressor>
	{
		[Fact]
		public async Task UnusedMethodSuppressedAsync()
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

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task UnusedParameterSuppressedAsync()
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
				.WithLocation(6, 35);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}
	}
}
