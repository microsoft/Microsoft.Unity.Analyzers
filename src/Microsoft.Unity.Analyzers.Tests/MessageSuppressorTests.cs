/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class MessageSuppressorTests : BaseSuppressorVerifierTest<MessageSuppressor>
	{
		[Fact]
		public async Task UnusedStaticMethodAndParametersSuppressed()
		{
			const string test = @"
using UnityEditor;

class Processor : AssetPostprocessor
{
    private static string OnGeneratedCSProject(string path, string content)
    {
        return null;
    }
}";

			var suppressors = new[] {
				ExpectSuppressor(MessageSuppressor.MethodRule).WithLocation(6, 27),
				ExpectSuppressor(MessageSuppressor.ParameterRule).WithLocation(6, 55),
				ExpectSuppressor(MessageSuppressor.ParameterRule).WithLocation(6, 68),
			};

			await VerifyCSharpDiagnosticAsync(test, suppressors);
		}

		[Fact]
		public async Task UnusedMethodSuppressed()
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

			var suppressor = ExpectSuppressor(MessageSuppressor.MethodRule)
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

			var suppressor = ExpectSuppressor(MessageSuppressor.ParameterRule)
				.WithLocation(6, 35);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}
	}
}
