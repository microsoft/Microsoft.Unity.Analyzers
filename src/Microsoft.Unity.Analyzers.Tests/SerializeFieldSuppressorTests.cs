/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class SerializeFieldSuppressorTests : BaseSuppressorVerifierTest<SerializeFieldSuppressor>
	{
		[Fact]
		public void NeverAssignedSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    private string someField;

    private void RemoveIDE0051() {
        var _ = someField;
        RemoveIDE0051();
    }
}
";

			var suppressor = ExpectSuppressor(SerializeFieldSuppressor.NeverAssignedRule)
				.WithLocation(7, 20);

			VerifyCSharpDiagnostic(test, suppressor);
		}

		[Fact]
		public void ReadonlySuppressed()
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
				.WithSuppressedDiagnosticMock(SyntaxKind.FieldDeclaration) // Use a mock while IDE analyzers have strong dependencies on Visual Studio components
				.WithLocation(7, 20);

			VerifyCSharpDiagnostic(test, suppressor);
		}

		[Fact]
		public void UnusedSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    private string someField = ""default"";
}
";

			var suppressor = ExpectSuppressor(SerializeFieldSuppressor.UnusedRule)
				.WithLocation(7, 20);

			VerifyCSharpDiagnostic(test, suppressor);
		}
	}
}
