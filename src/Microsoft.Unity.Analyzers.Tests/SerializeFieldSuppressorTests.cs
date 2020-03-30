/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class SerializeFieldSuppressorTests : BaseSuppressorVerifierTest<SerializeFieldSuppressor>
	{
		[Fact]
		public async Task NeverAssignedSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    readonly string someField; // we only use readonly here for testing purposes, suppressors are tested unitarily

    private void RemoveIDE0051() {
        var _ = someField;
        RemoveIDE0051();
    }
}
";

			var suppressor = ExpectSuppressor(SerializeFieldSuppressor.NeverAssignedRule)
				.WithLocation(7, 21);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task ReadonlySuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    private string someField = ""default"";

    private void RemoveIDE0051() {
        var _ = someField;
        RemoveIDE0051();
    }
}
";

			var suppressor = ExpectSuppressor(SerializeFieldSuppressor.ReadonlyRule)
				.WithLocation(7, 20);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task UnusedSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    private  string someField = ""default""; // we only use readonly here for testing purposes, suppressor are tested unitarily (so SerializeFieldSuppressor.ReadonlyRule [IDE0044] cannot be suppressed here)
}
";

			var suppressor = ExpectSuppressor(SerializeFieldSuppressor.UnusedRule)
				.WithLocation(7, 21);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}
	}
}
