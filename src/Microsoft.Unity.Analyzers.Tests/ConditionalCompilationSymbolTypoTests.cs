/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class ConditionalCompilationSymbolTypoTests : BaseCodeFixVerifierTest<ConditionalCompilationSymbolTypoAnalyzer, ConditionalCompilationSymbolTypoCodeFix>
{
	[Fact]
	public async Task UnityPlatformSymbolTypo()
	{
		const string test = @"
class Test
{
    void Method()
    {
#if UNITTY_STANDALONE_OSX
        return;
#endif
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 5)
			.WithArguments("UNITTY_STANDALONE_OSX", "UNITY_STANDALONE_OSX");

		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("UNITY_STANDALONE_OSX");

		await VerifyCSharpDiagnosticAsync(context, test, diagnostic);

		const string fixedTest = @"
class Test
{
    void Method()
    {
#if UNITY_STANDALONE_OSX
        return;
#endif
    }
}
";

		await VerifyCSharpFixAsync(context, test, fixedTest);
	}

	[Fact]
	public async Task CompoundConditionTypoTrivia()
	{
		const string test = @"
class Test
{
    void Method()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || !UNITTY_STANDALONE_OSX
        return;
#endif
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 72)
			.WithArguments("UNITTY_STANDALONE_OSX", "UNITY_STANDALONE_OSX");

		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("UNITY_EDITOR", "UNITY_STANDALONE_WIN", "UNITY_STANDALONE_LINUX", "UNITY_STANDALONE_OSX");

		await VerifyCSharpDiagnosticAsync(context, test, diagnostic);

		const string fixedTest = @"
class Test
{
    void Method()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || !UNITY_STANDALONE_OSX
        return;
#endif
    }
}
";

		await VerifyCSharpFixAsync(context, test, fixedTest);
	}

	[Fact]
	public async Task ElifDirectiveTypo()
	{
		const string test = @"
class Test
{
    void Method()
    {
#if UNITY_EDITOR
        return;
#elif UNITTY_STANDALONE_OSX
        return;
#endif
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 7)
			.WithArguments("UNITTY_STANDALONE_OSX", "UNITY_STANDALONE_OSX");

		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("UNITY_EDITOR", "UNITY_STANDALONE_OSX");

		await VerifyCSharpDiagnosticAsync(context, test, diagnostic);
	}

	[Fact]
	public async Task ProjectSymbolTypo()
	{
		const string test = @"
class Test
{
    void Method()
    {
#if FEATURE_RELEAS
        return;
#endif
    }
}
";

		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("FEATURE_RELEASE");

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 5)
			.WithArguments("FEATURE_RELEAS", "FEATURE_RELEASE");

		await VerifyCSharpDiagnosticAsync(context, test, diagnostic);
	}

	[Fact]
	public async Task DefinedProjectSymbol()
	{
		const string test = @"
class Test
{
    void Method()
    {
#if FEATURE_RELEASE
        return;
#endif
    }
}
";

		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("FEATURE_RELEASE");

		await VerifyCSharpDiagnosticAsync(context, test);
	}

	[Fact]
	public async Task DistantUndefinedSymbol()
	{
		const string test = @"
class Test
{
    void Method()
    {
#if OTHER_SYMBOL
        return;
#endif
    }
}
";

		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("FEATURE_RELEASE");

		await VerifyCSharpDiagnosticAsync(context, test);
	}

	[Fact]
	public async Task LocallyDefinedSymbol()
	{
		const string test = @"
#define UNITTY_STANDALONE_OSX

class Test
{
    void Method()
    {
#if UNITTY_STANDALONE_OSX
        return;
#endif
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

}
