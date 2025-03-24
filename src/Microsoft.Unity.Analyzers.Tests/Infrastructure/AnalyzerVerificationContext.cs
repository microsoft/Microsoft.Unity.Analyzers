/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Unity.Analyzers.Tests;

public readonly struct AnalyzerVerificationContext(ImmutableDictionary<string, string> options, ImmutableArray<string> filters, LanguageVersion languageVersion)
{
	public ImmutableDictionary<string, string> Options { get; } = options;
	public ImmutableArray<string> Filters { get; } = filters;
	public LanguageVersion LanguageVersion { get; } = languageVersion;

	public static AnalyzerVerificationContext Default = new(
		ImmutableDictionary<string, string>.Empty,
		[],
		LanguageVersion.Latest);

	public AnalyzerVerificationContext WithAnalyzerOption(string key, string value)
	{
		return new(
			Options.Add(key, value),
			Filters,
			LanguageVersion);
	}

	public AnalyzerVerificationContext WithAnalyzerFilter(string value)
	{
		return new(
			Options,
			Filters.Add(value),
			LanguageVersion);
	}

	public AnalyzerVerificationContext WithLanguageVersion(LanguageVersion languageVersion)
	{
		return new(
			Options,
			Filters,
			languageVersion);
	}
}
