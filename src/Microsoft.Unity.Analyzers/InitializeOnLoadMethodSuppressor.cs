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

			if (!(node is MethodDeclarationSyntax method))
				return;

			var model = context.GetSemanticModel(diagnostic.Location.SourceTree);
			if (!(model.GetDeclaredSymbol(method) is IMethodSymbol methodSymbol))
				return;

			if (IsSuppressable(methodSymbol))
				context.ReportSuppression(Suppression.Create(Rule, diagnostic));
		}

		private static bool IsSuppressable(ISymbol symbol)
		{
			return symbol
				.GetAttributes()
				.Any(a => IsInitializeOnLoadMethodAttributeType(a.AttributeClass));
		}

		private static bool IsInitializeOnLoadMethodAttributeType(ITypeSymbol type)
		{
			return type.Matches(typeof(UnityEditor.InitializeOnLoadMethodAttribute))
				|| type.Matches(typeof(UnityEngine.RuntimeInitializeOnLoadMethodAttribute));
		}
	}
}
