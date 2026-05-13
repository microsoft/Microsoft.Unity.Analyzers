/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Unity.Analyzers.Tests;

public readonly struct AnalyzerVerificationContext(ImmutableDictionary<string, string> options, ImmutableArray<string> filters, LanguageVersion languageVersion, ImmutableArray<string> preprocessorSymbols)
{
	public ImmutableDictionary<string, string> Options { get; } = options;
	public ImmutableArray<string> Filters { get; } = filters;
	public LanguageVersion LanguageVersion { get; } = languageVersion;
	public ImmutableArray<string> PreprocessorSymbols { get; } = preprocessorSymbols;

	// CS0414 - cf. IDE0051
	public static AnalyzerVerificationContext Default = new(
		[],
		["CS0414"],
		LanguageVersion.Latest,
		[]);

	public AnalyzerVerificationContext WithAnalyzerOption(string key, string value)
	{
		return new(
			Options.Add(key, value),
			Filters,
			LanguageVersion,
			PreprocessorSymbols);
	}

	public AnalyzerVerificationContext WithAnalyzerFilter(string value)
	{
		return new(
			Options,
			Filters.Add(value),
			LanguageVersion,
			PreprocessorSymbols);
	}

	public AnalyzerVerificationContext WithLanguageVersion(LanguageVersion languageVersion)
	{
		return new(
			Options,
			Filters,
			languageVersion,
			PreprocessorSymbols);
	}

	public AnalyzerVerificationContext WithPreprocessorSymbols(params string[] symbols)
	{
		return new(
			Options,
			Filters,
			LanguageVersion,
			[.. symbols]);
	}
}
