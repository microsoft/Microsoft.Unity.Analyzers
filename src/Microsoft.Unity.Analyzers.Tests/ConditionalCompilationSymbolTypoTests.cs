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
			.WithPreprocessorSymbols("UNITY_EDITOR", "UNITY_EDITOR_OSX", "UNITY_STANDALONE", "UNITY_STANDALONE_OSX", "UNITY_64");

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

	[Theory]
	[InlineData("EATURE_RELEASE", "FEATURE_RELEASE")]
	[InlineData("FEATURE_RELESE", "FEATURE_RELEASE")]
	[InlineData("FEATURE_RELEAS", "FEATURE_RELEASE")]
	[InlineData("FFEATURE_RELEASE", "FEATURE_RELEASE")]
	[InlineData("FEATTURE_RELEASE", "FEATURE_RELEASE")]
	[InlineData("FEATURE_RELEASEE", "FEATURE_RELEASE")]
	[InlineData("GEATURE_RELEASE", "FEATURE_RELEASE")]
	[InlineData("FEATURE_RELEAZE", "FEATURE_RELEASE")]
	[InlineData("FEATURE_RELEASX", "FEATURE_RELEASE")]
	[InlineData("EFATURE_RELEASE", "FEATURE_RELEASE")]
	[InlineData("FETAURE_RELEASE", "FEATURE_RELEASE")]
	[InlineData("FEATURE_RELEAES", "FEATURE_RELEASE")]
	[InlineData("UNITY_EDTIOR", "UNITY_EDITOR")]
	[InlineData("UNITY_6000_3_OR_NEWRE", "UNITY_6000_3_OR_NEWER")]
	[InlineData("UNITY_6000__3_OR_NEWER", "UNITY_6000_3_OR_NEWER")]
	[InlineData("UNITY_6000_3_OR_NEWE", "UNITY_6000_3_OR_NEWER")]
	[InlineData("UNITY_6000_3_OR_NEWER_", "UNITY_6000_3_OR_NEWER")]
	[InlineData("FEATURE_12_V2_RELEAS", "FEATURE_12_V2_RELEASE")]
	[InlineData("FEATURE_1A2", "FEATURE_1B2")]
	[InlineData("ACB", "ABC")]
	public async Task SingleTypo(string symbol, string definedSymbol)
	{
		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols(definedSymbol);

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 5)
			.WithArguments(symbol, definedSymbol);

		await VerifyCSharpDiagnosticAsync(context, Source(symbol), diagnostic);
		await VerifyCSharpFixAsync(context, Source(symbol), Source(definedSymbol));
	}

	[Theory]
	[InlineData("FEATURE_RELXXSE")]
	[InlineData("FFEATURE_RELEASEE")]
	[InlineData("FEATURE_RELEASXX")]
	[InlineData("FETAURE_RELEAS")]
	public async Task MultipleEdits(string symbol)
	{
		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("FEATURE_RELEASE");

		await VerifyCSharpDiagnosticAsync(context, Source(symbol));
	}

	[Theory]
	[InlineData("FEATURE_1", "FEATURE_2")]
	[InlineData("FEATURE_1", "FEATURE_12")]
	[InlineData("FEATURE_12", "FEATURE_1")]
	[InlineData("FEATURE_12", "FEATURE_21")]
	[InlineData("FEATURE_01", "FEATURE_1")]
	[InlineData("FEATURE", "FEATURE1")]
	[InlineData("FEATURE1", "FEATURE")]
	[InlineData("FEATURE_1_2", "FEATURE_12")]
	[InlineData("FEATURE_12", "FEATURE_1_2")]
	[InlineData("FEATURE_1A2", "FEATURE_12")]
	[InlineData("FEATURE_V2_X3", "FEATURE_V2_X4")]
	[InlineData("FEATURE_V2_X3", "FEATURE_V2_X")]
	[InlineData("FEATURE_V2_X", "FEATURE_V2_X3")]
	public async Task DifferentNumericComponents(string symbol, string definedSymbol)
	{
		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols(definedSymbol);

		await VerifyCSharpDiagnosticAsync(context, Source(symbol));
	}

	[Theory]
	[InlineData("UNITY_5")]
	[InlineData("UNITY_2019_4")]
	[InlineData("UNITY_2019_4_40")]
	[InlineData("UNITY_6000_2")]
	[InlineData("UNITY_6000_3_8")]
	[InlineData("UNITY_6000_4_OR_NEWER")]
	[InlineData("UNITY_6000_5_OR_NEWER")]
	[InlineData("UNITY_9999_9_OR_NEWER")]
	public async Task InactiveUnityVersionSymbol(string symbol)
	{
		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("UNITY_6000", "UNITY_6000_3", "UNITY_6000_3_9",
				"UNITY_6000_0_OR_NEWER", "UNITY_6000_1_OR_NEWER", "UNITY_6000_2_OR_NEWER", "UNITY_6000_3_OR_NEWER");

		await VerifyCSharpDiagnosticAsync(context, Source(symbol));
	}

	[Theory]
	[InlineData("UNITY_6000")]
	[InlineData("UNITY_6000_4")]
	[InlineData("UNITY_6000_4_1")]
	[InlineData("UNITY_6000_4_OR_NEWER")]
	public async Task UnityVersionSymbolResemblingCustomSymbol(string symbol)
	{
		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols(symbol + "_");

		await VerifyCSharpDiagnosticAsync(context, Source(symbol));
	}

	[Theory]
	[InlineData("UNITY_PS4 || UNITY_PS5")]
	[InlineData("UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX")]
	[InlineData("UNITY_EDITOR || UNITY_STANDALONE || UNITY_PS4 || UNITY_PS5 || UNITY_WSA || UNITY_ANDROID || UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS")]
	public async Task InactivePlatformSymbols(string condition)
	{
		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("UNITY_EDITOR", "UNITY_EDITOR_WIN", "UNITY_STANDALONE", "UNITY_STANDALONE_WIN", "UNITY_64");

		await VerifyCSharpDiagnosticAsync(context, Source(condition));
	}

	[Fact]
	public async Task InactiveVersionSymbolsInElif()
	{
		const string test = @"
class Test
{
    void Method()
    {
#if UNITY_PS4 || UNITY_PS5
        return;
#elif !UNITY_6000_4_OR_NEWER && !UNITY_6000_5_OR_NEWER
        return;
#endif
    }
}
";

		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("UNITY_STANDALONE", "UNITY_STANDALONE_WIN", "UNITY_64", "UNITY_6000_3_OR_NEWER");

		await VerifyCSharpDiagnosticAsync(context, test);
	}

	[Theory]
	[InlineData("FEATURE_RELEASX", "FEATURE_RELEASE", "FEATURE_RELEASY")]
	[InlineData("FEATURE_RELEAS", "FEATURE_RELEASE", "FEATURE_RELEAZ")]
	[InlineData("UNITY_EDTIOR", "UNITY_EDITOR", "UNITY_EDTIOS")]
	public async Task AmbiguousSymbol(string symbol, string firstCandidate, string secondCandidate)
	{
		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols(firstCandidate, secondCandidate);

		await VerifyCSharpDiagnosticAsync(context, Source(symbol));

		context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols(secondCandidate, firstCandidate);

		await VerifyCSharpDiagnosticAsync(context, Source(symbol));
	}

	[Fact]
	public async Task DuplicateDefinedSymbolsAreNotAmbiguous()
	{
		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("FEATURE_RELEASE", "FEATURE_RELEASE");

		var diagnostic = ExpectDiagnostic()
			.WithLocation(6, 5)
			.WithArguments("FEATURE_RELEAS", "FEATURE_RELEASE");

		await VerifyCSharpDiagnosticAsync(context, Source("FEATURE_RELEAS"), diagnostic);
	}

	[Fact]
	public async Task TypoForInactivePlatform()
	{
		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("UNITY_EDITOR", "UNITY_EDITOR_WIN", "UNITY_STANDALONE", "UNITY_STANDALONE_WIN", "UNITY_64");

		await VerifyCSharpDiagnosticAsync(context, Source("UNITTY_STANDALONE_OSX"));
	}

	[Fact]
	public async Task ShortSymbol()
	{
		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("ABC");

		await VerifyCSharpDiagnosticAsync(context, Source("AB"));
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

		var context = AnalyzerVerificationContext.Default
			.WithPreprocessorSymbols("UNITY_STANDALONE_OSX");

		await VerifyCSharpDiagnosticAsync(context, test);
	}

	private static string Source(string condition)
	{
		return $@"
class Test
{{
    void Method()
    {{
#if {condition}
        return;
#endif
    }}
}}
";
	}
}
