/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnusedMessageSuppressor : DiagnosticSuppressor
	{
		private static readonly SuppressionDescriptor MethodRule = new SuppressionDescriptor(
			id: "USP0003",
			suppressedDiagnosticId: "IDE0051",
			justification: Strings.UnusedMessageSuppressorJustification);

		private static readonly SuppressionDescriptor ParameterRule = new SuppressionDescriptor(
			id: "USP0005",
			suppressedDiagnosticId: "IDE0060",
			justification: Strings.UnusedMessageSuppressorJustification);

		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(MethodRule, ParameterRule);

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

			if (node is ParameterSyntax)
				node = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

			if (!(node is MethodDeclarationSyntax method))
				return;

			var model = context.GetSemanticModel(diagnostic.Location.SourceTree);
			if (!(model.GetDeclaredSymbol(method) is IMethodSymbol methodSymbol))
				return;

			var scriptInfo = new ScriptInfo(methodSymbol.ContainingType);
			if (!scriptInfo.IsMessage(methodSymbol))
				return;

			foreach(var suppression in SupportedSuppressions)
			{
				if (suppression.SuppressedDiagnosticId == diagnostic.Id)
					context.ReportSuppression(Suppression.Create(suppression, diagnostic));
			}
		}
	}
}
