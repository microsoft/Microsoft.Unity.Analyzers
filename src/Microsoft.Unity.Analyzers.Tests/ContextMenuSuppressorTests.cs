/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class ContextMenuSuppressorTests : BaseSuppressorVerifierTest<ContextMenuSuppressor>
	{
		[Fact]
		public void ContextMenuSuppressed()
		{
			const string test = @"
using UnityEngine;
using UnityEditor;

class Menu : MonoBehaviour
{
    [ContextMenu(""Context Menu Name"")]
    private void ContextMenuMethod()
    {
    }
}";

			var suppressor = ExpectSuppressor(ContextMenuSuppressor.ContextMenuRule)
				.WithLocation(8, 18);

			VerifyCSharpDiagnostic(test, suppressor);
		}

		[Fact]
		public void ContextMenuItemReadonlySuppressed()
		{
			const string test = @"
using UnityEngine;
using UnityEditor;

class Menu : MonoBehaviour
{
    [ContextMenuItem(""Foo"", ""Bar"")]
    private string contextMenuString = """";

    private void RemoveIDE0051() {
        var _ = contextMenuString;
        RemoveIDE0051();
    }
}";

			var suppressor = ExpectSuppressor(ContextMenuSuppressor.ContextMenuItemReadonlyRule)
				.WithSuppressedDiagnosticMock(SyntaxKind.FieldDeclaration) // Use a mock while IDE analyzers have strong dependencies on Visual Studio components
				.WithLocation(8, 20);

			VerifyCSharpDiagnostic(test, suppressor);
		}

		[Fact]
		public void ContextMenuItemUnusedSuppressed()
		{
			const string test = @"
using UnityEngine;
using UnityEditor;

class Menu : MonoBehaviour
{
    [ContextMenuItem(""Foo"", ""Bar"")]
    private string contextMenuString = """";
}";

			var suppressor = ExpectSuppressor(ContextMenuSuppressor.ContextMenuItemUnusedRule)
				.WithLocation(8, 20);

			VerifyCSharpDiagnostic(test, suppressor);
		}

		[Fact]
		public void ContextMenuItemReferenceUnusedSuppressed()
		{
			const string test = @"
using UnityEngine;
using UnityEditor;

class Menu : MonoBehaviour
{
    [ContextMenuItem(""Reset Health"", ""ResetHealth"")]
    public int health;

    private void ResetHealth()
    {
        health = 100;
    }
}";

			var suppressor = ExpectSuppressor(ContextMenuSuppressor.ContextMenuRule)
				.WithLocation(10, 18);

			VerifyCSharpDiagnostic(test, suppressor);
		}

	}
}
