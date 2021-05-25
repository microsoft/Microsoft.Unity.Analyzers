/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class InputGetKeyTests : BaseCodeFixVerifierTest<InputGetKeyAnalyzer, InputGetKeyCodeFix>
	{
		[Fact]
		public async Task TestMember()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKey(""a""))
            return;
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 13);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKey(KeyCode.A))
            return;
    }
}
";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task TestGetKeyUpLowercase()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyUp(""a""))
            return;
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 13);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
            return;
    }
}
";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task TestGetKeyDownUppercase()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(""BACKSPACE""))
            return;
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 13);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Backspace))
            return;
    }
}
";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task TestMappedMember()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKey(""joystick 4 button 2""))
            return;
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 13);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKey(KeyCode.Joystick4Button2))
            return;
    }
}
";

			await VerifyCSharpFixAsync(test, fixedTest);
		}


		[Fact]
		public async Task TestTrivia()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        if (/* outer before */ Input.GetKey(/* inner before */ ""a"" /* inner after */) /* outer after */)
            return;
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 32);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        if (/* outer before */ Input.GetKey(/* inner before */ KeyCode.A /* inner after */) /* outer after */)
            return;
    }
}
";

			await VerifyCSharpFixAsync(test, fixedTest);
		}


		[Fact]
		public void TestMappings()
		{
			var names = new[]
			{
				"backspace", "delete", "tab", "clear", "return", "pause", "escape", "space",
				"[0]", "[1]", "[2]", "[3]", "[4]", "[5]", "[6]", "[7]", "[8]", "[9]", "[.]", "[/]", "[*]", "[-]", "[+]",
				"equals", "enter", "up", "down", "right", "left", "insert", "home", "end", "page up", "page down",
				"f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12", "f13", "f14", "f15",
				"0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
				"-", "=", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "_", "+", "[", "]", "`", "{", "}", "~", ";", "'", @"\", ":", "\"", "|", ",", ".", "/", "<", ">", "?",
				"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
				"numlock", "caps lock", "scroll lock",
				"right shift", "left shift",
				"right ctrl", "left ctrl",
				"right alt", "left alt",
				"right cmd", "left cmd",
				/*"right super", "left super",*/
				"alt gr", /*"compose",*/ "help", "print screen", "sys req", "break", "menu", /*"power", "euro", "undo",*/
				"mouse 0", "mouse 1", "mouse 2", "mouse 3", "mouse 4", "mouse 5", "mouse 6",
				"joystick button 0", "joystick button 1", "joystick button 2", "joystick button 3", "joystick button 4", "joystick button 5", "joystick button 6", "joystick button 7", "joystick button 8", "joystick button 9", "joystick button 10", "joystick button 11", "joystick button 12", "joystick button 13", "joystick button 14", "joystick button 15", "joystick button 16", "joystick button 17", "joystick button 18", "joystick button 19", "joystick 1 button 0", "joystick 1 button 1", "joystick 1 button 2", "joystick 1 button 3", "joystick 1 button 4", "joystick 1 button 5", "joystick 1 button 6", "joystick 1 button 7", "joystick 1 button 8", "joystick 1 button 9", "joystick 1 button 10", "joystick 1 button 11", "joystick 1 button 12", "joystick 1 button 13", "joystick 1 button 14", "joystick 1 button 15", "joystick 1 button 16", "joystick 1 button 17", "joystick 1 button 18", "joystick 1 button 19", "joystick 2 button 0", "joystick 2 button 1", "joystick 2 button 2", "joystick 2 button 3", "joystick 2 button 4", "joystick 2 button 5", "joystick 2 button 6", "joystick 2 button 7", "joystick 2 button 8", "joystick 2 button 9", "joystick 2 button 10", "joystick 2 button 11", "joystick 2 button 12", "joystick 2 button 13", "joystick 2 button 14", "joystick 2 button 15", "joystick 2 button 16", "joystick 2 button 17", "joystick 2 button 18", "joystick 2 button 19", "joystick 3 button 0", "joystick 3 button 1", "joystick 3 button 2", "joystick 3 button 3", "joystick 3 button 4", "joystick 3 button 5", "joystick 3 button 6", "joystick 3 button 7", "joystick 3 button 8", "joystick 3 button 9", "joystick 3 button 10", "joystick 3 button 11", "joystick 3 button 12", "joystick 3 button 13", "joystick 3 button 14", "joystick 3 button 15", "joystick 3 button 16", "joystick 3 button 17", "joystick 3 button 18", "joystick 3 button 19", "joystick 4 button 0", "joystick 4 button 1", "joystick 4 button 2", "joystick 4 button 3", "joystick 4 button 4", "joystick 4 button 5", "joystick 4 button 6", "joystick 4 button 7", "joystick 4 button 8", "joystick 4 button 9", "joystick 4 button 10", "joystick 4 button 11", "joystick 4 button 12", "joystick 4 button 13", "joystick 4 button 14", "joystick 4 button 15", "joystick 4 button 16", "joystick 4 button 17", "joystick 4 button 18", "joystick 4 button 19", "joystick 5 button 0", "joystick 5 button 1", "joystick 5 button 2", "joystick 5 button 3", "joystick 5 button 4", "joystick 5 button 5", "joystick 5 button 6", "joystick 5 button 7", "joystick 5 button 8", "joystick 5 button 9", "joystick 5 button 10", "joystick 5 button 11", "joystick 5 button 12", "joystick 5 button 13", "joystick 5 button 14", "joystick 5 button 15", "joystick 5 button 16", "joystick 5 button 17", "joystick 5 button 18", "joystick 5 button 19", "joystick 6 button 0", "joystick 6 button 1", "joystick 6 button 2", "joystick 6 button 3", "joystick 6 button 4", "joystick 6 button 5", "joystick 6 button 6", "joystick 6 button 7", "joystick 6 button 8", "joystick 6 button 9", "joystick 6 button 10", "joystick 6 button 11", "joystick 6 button 12", "joystick 6 button 13", "joystick 6 button 14", "joystick 6 button 15", "joystick 6 button 16", "joystick 6 button 17", "joystick 6 button 18", "joystick 6 button 19", "joystick 7 button 0", "joystick 7 button 1", "joystick 7 button 2", "joystick 7 button 3", "joystick 7 button 4", "joystick 7 button 5", "joystick 7 button 6", "joystick 7 button 7", "joystick 7 button 8", "joystick 7 button 9", "joystick 7 button 10", "joystick 7 button 11", "joystick 7 button 12", "joystick 7 button 13", "joystick 7 button 14", "joystick 7 button 15", "joystick 7 button 16", "joystick 7 button 17", "joystick 7 button 18", "joystick 7 button 19", "joystick 8 button 0", "joystick 8 button 1", "joystick 8 button 2", "joystick 8 button 3", "joystick 8 button 4", "joystick 8 button 5", "joystick 8 button 6", "joystick 8 button 7", "joystick 8 button 8", "joystick 8 button 9", "joystick 8 button 10", "joystick 8 button 11", "joystick 8 button 12", "joystick 8 button 13", "joystick 8 button 14", "joystick 8 button 15", "joystick 8 button 16", "joystick 8 button 17", "joystick 8 button 18", "joystick 8 button 19",
			};

			foreach (var name in names)
			{
				var les = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name));
				Assert.True(InputGetKeyAnalyzer.TryParse(les, out _), $"Unable to map '{name}' to KeyCode enumeration member");
			}
		}
	}
}
