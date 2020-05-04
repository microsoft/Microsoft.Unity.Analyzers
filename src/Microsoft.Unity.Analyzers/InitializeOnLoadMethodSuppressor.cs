/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class InitializeOnLoadMethodSuppressor : DiagnosticSuppressor
	{
		internal static readonly SuppressionDescriptor Rule = new SuppressionDescriptor(
			id: "USP0012",
			suppressedDiagnosticId: "IDE0051",
			justification: Strings.InitializeOnLoadMethodSuppressorJustification);

		public override void ReportSuppressions(SuppressionAnalysisContext context)
		{
			foreach (var diagnostic in context.ReportedDiagnostics)
				AnalyzeDiagnostic(diagnostic, context);
		}

		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(Rule);

		private static void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
		{
			var sourceTree = diagnostic.Location.SourceTree;
			var root = sourceTree.GetRoot(context.CancellationToken);
			var node = root.FindNode(diagnostic.Location.SourceSpan);
			var model = context.GetSemanticModel(diagnostic.Location.SourceTree);

			// Reuse the same detection logic regarding decorated methods with *InitializeOnLoadMethodAttribute
			if (InitializeOnLoadMethodAnalyzer.MethodMatches(node, model, out _, out _))
				context.ReportSuppression(Suppression.Create(Rule, diagnostic));
		}
	}
}
