﻿/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public abstract class SuppressorVerifier : DiagnosticVerifier
	{
		protected static DiagnosticResult ExpectSuppressor(SuppressionDescriptor descriptor)
		{
			var result = new DiagnosticResult(descriptor.Id, DiagnosticSeverity.Hidden)
				.WithMessageFormat(descriptor.Justification)
				.WithSuppressedId(descriptor.SuppressedDiagnosticId);
			
			return result;
		}

		private IEnumerable<DiagnosticAnalyzer> LoadAnalyzers(string assembly)
		{
			var reference = new AnalyzerFileReference(assembly, new AnalyzerAssemblyLoader());
			reference.AnalyzerLoadFailed += (s, e) => { Assert.True(false, e.Message); };
			return reference.GetAnalyzers(LanguageNames.CSharp);
		}

		protected override IEnumerable<DiagnosticAnalyzer> GetRelatedAnalyzers(DiagnosticAnalyzer analyzer)
		{
			var suppressor = (DiagnosticSuppressor)analyzer;

			var analyzers = new List<DiagnosticAnalyzer>();
			analyzers.AddRange(LoadAnalyzers("Microsoft.CodeAnalysis.CodeStyle.dll"));
			analyzers.AddRange(LoadAnalyzers("Microsoft.CodeAnalysis.CSharp.CodeStyle.dll"));

			return analyzers.Where(a => a.SupportedDiagnostics
				.Any(s => suppressor.SupportedSuppressions
					.Any(sp => sp.SuppressedDiagnosticId == s.Id)));
		}

		private static bool IsSuppressedBy(Diagnostic diagnostic, DiagnosticResult suppressor)
		{
			if (!diagnostic.IsSuppressed)
				return false;

			if (string.IsNullOrEmpty(suppressor.SuppressedId))
				return false;

			if (diagnostic.Id != suppressor.SuppressedId)
				return false;

			// Internal Roslyn info
			var psiProperty = diagnostic.GetType().GetProperty("ProgrammaticSuppressionInfo", BindingFlags.Instance | BindingFlags.NonPublic);
			if (psiProperty == null)
				return false;

			var psi = psiProperty.GetValue(diagnostic);
			if (psi == null)
				return false;

			var spProperty = psi.GetType().GetProperty("Suppressions");
			if (spProperty == null)
				return false;

			var suppressions = (ImmutableHashSet<(string Id, LocalizableString Justification)>)spProperty.GetValue(psi);
			if (!suppressions.Any(t => t.Id == suppressor.Id && t.Justification.Equals(suppressor.MessageFormat)))
				return false;

			return suppressor.Spans.Any(sp => sp.Span.StartLinePosition == diagnostic.Location.GetLineSpan().StartLinePosition);
		}

		protected override void VerifyDiagnosticResults(Diagnostic[] actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
		{
			var suppressed = actualResults
				.Where(d => expectedResults.Any(s => IsSuppressedBy(d, s)))
				.ToArray();

			actualResults = actualResults
				// Filter diagnostic with an effective suppression
				.Where(d => !suppressed.Contains(d))
				.ToArray();

			expectedResults = expectedResults
				// Filter suppressors effectively suppressing diagnostic
				.Where(s => !suppressed.Any(d => IsSuppressedBy(d, s)))
				.ToArray();

			base.VerifyDiagnosticResults(actualResults, analyzer, expectedResults);
		}
	}
}
