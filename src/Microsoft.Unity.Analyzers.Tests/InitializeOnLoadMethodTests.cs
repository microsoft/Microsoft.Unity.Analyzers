/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class InitializeOnLoadMethodTests : BaseCodeFixVerifierTest<InitializeOnLoadMethodAnalyzer, InitializeOnLoadMethodCodeFix>
	{
		[Fact]
		public async Task InitializeOnLoadMethodTest()
		{
			const string test = @"
using UnityEditor;

class Loader
{
    [InitializeOnLoadMethod]
    private static void OnLoad() {}
}";

			await VerifyCSharpDiagnosticAsync(test);
		}

		[Fact]
		public async Task InitializeOnLoadMethodFixModifiersTest()
		{
			const string test = @"
using UnityEditor;

class Loader
{
    [InitializeOnLoadMethod]
    private void OnLoad()
    {
        // keep
    }
}";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(7, 18)
				.WithArguments("OnLoad");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEditor;

class Loader
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        // keep
    }
}";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task InitializeOnLoadMethodFixParametersTest()
		{
			const string test = @"
using UnityEditor;

class Loader
{
    [InitializeOnLoadMethod]
    private static void OnLoad(int foo, string bar)
    {
        // keep
    }
}";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(7, 25)
				.WithArguments("OnLoad");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEditor;

class Loader
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        // keep
    }
}";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task InitializeOnLoadMethodFixAllTest()
		{
			const string test = @"
using UnityEditor;

class Loader
{
    [InitializeOnLoadMethod]
    private void OnLoad(int foo, string bar)
    {
        // keep
    }
}";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(7, 18)
				.WithArguments("OnLoad");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEditor;

class Loader
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        // keep
    }
}";

			await VerifyCSharpFixAsync(test, fixedTest);
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
}";

			await VerifyCSharpDiagnosticAsync(test);
		}

		[Fact]
		public async Task RuntimeInitializeOnLoadMethodFixModifiersTest()
		{
			const string test = @"
using UnityEngine;

class Loader
{
    [RuntimeInitializeOnLoadMethod]
    private void OnLoad()
    {
        // keep
    }
}";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(7, 18)
				.WithArguments("OnLoad");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Loader
{
    [RuntimeInitializeOnLoadMethod]
    private static void OnLoad()
    {
        // keep
    }
}";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task RuntimeInitializeOnLoadMethodFixParametersTest()
		{
			const string test = @"
using UnityEngine;

class Loader
{
    [RuntimeInitializeOnLoadMethod]
    private static void OnLoad(int foo, string bar)
    {
        // keep
    }
}";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(7, 25)
				.WithArguments("OnLoad");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Loader
{
    [RuntimeInitializeOnLoadMethod]
    private static void OnLoad()
    {
        // keep
    }
}";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task RuntimeInitializeOnLoadMethodFixAllTest()
		{
			const string test = @"
using UnityEngine;

class Loader
{
    [RuntimeInitializeOnLoadMethod]
    private void OnLoad(int foo, string bar)
    {
        // keep
    }
}";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(7, 18)
				.WithArguments("OnLoad");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Loader
{
    [RuntimeInitializeOnLoadMethod]
    private static void OnLoad()
    {
        // keep
    }
}";

			await VerifyCSharpFixAsync(test, fixedTest);
		}
	}
}
