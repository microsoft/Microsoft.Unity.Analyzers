/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class InitializeOnLoadMethodSuppressorTests : BaseSuppressorVerifierTest<InitializeOnLoadMethodSuppressor>
	{
		[Fact]
		public async Task InitializeOnLoadMethodTest()
		{
			const string test = @"
using UnityEditor;

class Loader
{
    [InitializeOnLoadMethod]
    private static void OnLoad() {
    }
}
";

			var suppressor = ExpectSuppressor(InitializeOnLoadMethodSuppressor.Rule)
				.WithLocation(7, 25);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task RuntimeInitializeOnLoadMethodTest()
		{
			const string test = @"
using UnityEngine;

class Loader
{
    [RuntimeInitializeOnLoadMethod]
    private static void OnLoad() {
    }
}
";

			var suppressor = ExpectSuppressor(InitializeOnLoadMethodSuppressor.Rule)
				.WithLocation(7, 25);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}
	}
}
