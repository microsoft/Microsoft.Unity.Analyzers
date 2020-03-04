/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class SerializeFieldSuppressor : DiagnosticSuppressor
	{
		internal static readonly SuppressionDescriptor ReadonlyRule = new SuppressionDescriptor(
			id: "USP0004",
			suppressedDiagnosticId: "IDE0044",
			justification: Strings.ReadonlySerializeFieldSuppressorJustification);

		internal static readonly SuppressionDescriptor UnusedRule = new SuppressionDescriptor(
			id: "USP0006",
			suppressedDiagnosticId: "IDE0051",
			justification: Strings.UnusedSerializeFieldSuppressorJustification);

		// Note that this one, is only used for full builds in VS
		internal static readonly SuppressionDescriptor NeverAssignedRule = new SuppressionDescriptor(
			id: "USP0007",
			suppressedDiagnosticId: "CS0649",
			justification: Strings.NeverAssignedSerializeFieldSuppressorJustification);

		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(ReadonlyRule, UnusedRule, NeverAssignedRule);

		public override void ReportSuppressions(SuppressionAnalysisContext context)
		{
			foreach (var diagnostic in context.ReportedDiagnostics)
			{
				AnalyzeDiagnostic(diagnostic, context);
			}
		}

		private void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
		{
			var node = diagnostic.Location.SourceTree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);
			if (node == null)
				return;

			var model = context.GetSemanticModel(diagnostic.Location.SourceTree);
			if (!(model.GetDeclaredSymbol(node) is IFieldSymbol fieldSymbol))
				return;

			if (!fieldSymbol.GetAttributes().Any(a => a.AttributeClass.Matches(typeof(UnityEngine.SerializeField)) || a.AttributeClass.Matches(typeof(UnityEngine.SerializeReference))))
				return;

			foreach (var descriptor in SupportedSuppressions.Where(d => d.SuppressedDiagnosticId == diagnostic.Id))
				context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
		}
	}
}
