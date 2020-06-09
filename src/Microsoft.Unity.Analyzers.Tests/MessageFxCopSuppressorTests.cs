/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class MessageFxCopSuppressorTests : BaseSuppressorVerifierTest<MessageSuppressor>
	{
		// Only load FxCop analyzers for those tests
		protected override SuppressorVerifierAnalyzers SuppressorVerifierAnalyzers => SuppressorVerifierAnalyzers.FxCop;

		[Fact]
		public async Task StaticMethodSuppressed()
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

			var suppressor = ExpectSuppressor(MessageSuppressor.MethodFxCopRule)
				.WithLocation(6, 18);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task UnusedParameterSuppressed()
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

			var suppressor = ExpectSuppressor(MessageSuppressor.ParameterFxCopRule)
				.WithLocation(6, 35);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}
	}
}
