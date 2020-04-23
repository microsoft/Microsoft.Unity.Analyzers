/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnusedMethodSuppressor : DiagnosticSuppressor
	{
		internal static readonly SuppressionDescriptor Rule = new SuppressionDescriptor(
			id: "USP0008",
			suppressedDiagnosticId: "IDE0051",
			justification: Strings.UnusedMethodSuppressorJustification);

		public override void ReportSuppressions(SuppressionAnalysisContext context)
		{
			foreach (var diagnostic in context.ReportedDiagnostics)
			{
				AnalyzeDiagnostic(diagnostic, context);
			}
		}

		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(Rule);

		private static void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
		{
			var sourceTree = diagnostic.Location.SourceTree;
			var root = sourceTree.GetRoot(context.CancellationToken);
			var node = root.FindNode(diagnostic.Location.SourceSpan);

			if (!(node is MethodDeclarationSyntax method))
				return;

			var model = context.GetSemanticModel(diagnostic.Location.SourceTree);
			if (!(model.GetDeclaredSymbol(method) is IMethodSymbol methodSymbol))
				return;

			var typeSymbol = methodSymbol.ContainingType;
			if (!typeSymbol.Extends(typeof(UnityEngine.MonoBehaviour)))
				return;

			var references = new List<InvocationExpressionSyntax>();
			foreach (var typeNode in typeSymbol.Locations.Select(location => root.FindNode(location.SourceSpan)))
			{
				references.AddRange(typeNode.DescendantNodes()
					.OfType<InvocationExpressionSyntax>()
					.Where(e => MethodInvocationAnalyzer.InvocationMatches(e, out string argument) && argument == methodSymbol.Name));
			}

			if (references.Any())
				context.ReportSuppression(Suppression.Create(Rule, diagnostic));
		}
	}
}
