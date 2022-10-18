﻿/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Unity.Analyzers.Tests;

public readonly struct AnalyzerVerificationContext
{
	public ImmutableDictionary<string, string> Options { get; }
	public ImmutableArray<string> Filters { get; }
	public LanguageVersion LanguageVersion { get; }
	public ImmutableArray<string> PreprocessorSymbols { get; }

	// CS1701 - Assuming assembly reference 'mscorlib, Version=2.0.0.0' used by 'UnityEngine' matches identity 'mscorlib, Version=4.0.0.0' of 'mscorlib', you may need to supply runtime policy
	// CS0414 - cf. IDE0051
	public static AnalyzerVerificationContext Default = new(
		ImmutableDictionary<string, string>.Empty,
		new[] {"CS1701", "CS0414"}.ToImmutableArray(),
		LanguageVersion.Latest,
		ImmutableArray<string>.Empty);

	public AnalyzerVerificationContext(ImmutableDictionary<string, string> options, ImmutableArray<string> filters, LanguageVersion languageVersion, ImmutableArray<string> preprocessorSymbols) : this()
	{
		Options = options;
		Filters = filters;
		LanguageVersion = languageVersion;
		PreprocessorSymbols = preprocessorSymbols;
	}

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

	public AnalyzerVerificationContext WithPreprocessorSymbol(string value)
	{
		return new(
			Options,
			Filters,
			LanguageVersion,
			PreprocessorSymbols.Add(value));
	}
}
