/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public abstract class SuppressorVerifier : DiagnosticVerifier
	{
		protected DiagnosticResult ExpectSuppressor(SuppressionDescriptor descriptor)
		{
			var result = new DiagnosticResult(descriptor.Id, DiagnosticSeverity.Hidden)
				.WithMessageFormat(descriptor.Justification)
				.WithSuppressedId(descriptor.SuppressedDiagnosticId);
			
			return result;
		}

		protected override IEnumerable<DiagnosticAnalyzer> GetExternalAnalyzers()
		{
			// Add IDExxxx diags that we want to check against our suppressors
			const string analyzerSet = @"Microsoft.CodeAnalysis.Csharp.Features.dll";
			var reference = new AnalyzerFileReference(analyzerSet, new AnalyzerAssemblyLoader());
			reference.AnalyzerLoadFailed += (s, e) =>
			{
				Assert.True(false, e.Message);
			};

			return reference.GetAnalyzers(LanguageNames.CSharp);
		}

		protected override void VerifyDiagnosticResults(Diagnostic[] actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
		{
			// Ignore external diagnostics failures, check for matching suppressions
			var suppressed = actualResults
				.Where(d => d.IsSuppressed)
				.ToArray();

			actualResults = actualResults
				.Where(d => d.Id != "AD0001")
				.Where(d => !(d.IsSuppressed && expectedResults.Any(s => s.SuppressedId == d.Id && s.Spans.Any(sp => sp.Span.StartLinePosition == d.Location.GetLineSpan().StartLinePosition) )))
				.ToArray();

			expectedResults = expectedResults
				.Where(s => string.IsNullOrEmpty(s.SuppressedId) || suppressed.All(d => d.Id != s.SuppressedId) )
				.ToArray();

			base.VerifyDiagnosticResults(actualResults, analyzer, expectedResults);
		}
	}
}
