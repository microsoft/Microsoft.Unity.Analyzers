/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class ContextMenuSuppressorTests : BaseSuppressorVerifierTest<ContextMenuSuppressor>
	{
		[Fact]
		public async Task ContextMenuSuppressed()
		{
			const string test = @"
using UnityEngine;

class Menu : MonoBehaviour
{
    [ContextMenu(""Context Menu Name"")]
    private void ContextMenuMethod()
    {
    }
}";

			var suppressor = ExpectSuppressor(ContextMenuSuppressor.ContextMenuRule)
				.WithLocation(7, 18);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task MenuItemSuppressed()
		{
			const string test = @"
using UnityEditor;
using UnityEngine;

class Menu : MonoBehaviour
{
    [MenuItem(""Tools/Project Reference Generator"")]
    private static void MenuItemMethod()
    {
    }
}";

			var suppressor = ExpectSuppressor(ContextMenuSuppressor.ContextMenuRule)
				.WithLocation(8, 25);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task ContextMenuItemReadonlySuppressed()
		{
			const string test = @"
using UnityEngine;

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
				.WithLocation(7, 20);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task ContextMenuItemUnusedSuppressed()
		{
			const string test = @"
using UnityEngine;

class Menu : MonoBehaviour
{
    [ContextMenuItem(""Foo"", ""Bar"")]
    readonly string contextMenuString = """"; // we only use readonly here for testing purposes, suppressor is tested unitarily (so ContextMenuSuppressor.ContextMenuItemReadonlyRule [IDE0044] cannot be suppressed here)
}";

			var suppressor = ExpectSuppressor(ContextMenuSuppressor.ContextMenuItemUnusedRule)
				.WithLocation(7, 21);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task ContextMenuItemReferenceUnusedSuppressed()
		{
			const string test = @"
using UnityEngine;

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
				.WithLocation(9, 18);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		/////
		///
		[Fact]
		public async Task OutsideMonoBehaviourMenuItemSuppressed()
		{
			const string test = @"
using UnityEditor;

static class TestCode
{
    [MenuItem(""Test/Hello World"")]
    static void DoMenuItem()
    {
        EditorUtility.DisplayDialog(""Hello"", ""World"", ""OK"");
    }
}";

			var suppressor = ExpectSuppressor(ContextMenuSuppressor.ContextMenuRule)
				.WithLocation(7, 17);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

	}
}
