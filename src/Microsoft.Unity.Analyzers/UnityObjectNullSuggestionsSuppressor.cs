using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	class UnityObjectNullSuggestionsSuppressor : DiagnosticSuppressor
	{
		private static readonly SuppressionDescriptor NullCoalescingRule = new SuppressionDescriptor(
			id: "USP0001",
			suppressedDiagnosticId: "IDE0029",
			justification: Strings.UnityObjectNullCoalescingSuppressorJustification);

		private static readonly SuppressionDescriptor NullPropagationRule = new SuppressionDescriptor(
			id: "USP0002",
			suppressedDiagnosticId: "IDE0031",
			justification: Strings.UnityObjectNullPropagationSuppressorJustification);

		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(NullCoalescingRule, NullPropagationRule);

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

			if (!node.IsKind(SyntaxKind.ConditionalExpression))
				return;

			var cond = (ConditionalExpressionSyntax)node;
			switch (cond.Condition.Kind())
			{
				case SyntaxKind.EqualsExpression:
				case SyntaxKind.NotEqualsExpression:
					break;
				default:
					return;
			}

			var binary = (BinaryExpressionSyntax)cond.Condition;
			if (!binary.Right.IsKind(SyntaxKind.NullLiteralExpression))
				return;

			var model = context.GetSemanticModel(node.SyntaxTree);
			if (model == null)
				return;

			var type = model.GetTypeInfo(binary.Left);
			if (type.Type == null)
				return;

			if (!UnityObjectNullCoalescingAnalyzer.IsUnityObject(type.Type))
				return;

			if (diagnostic.Id == NullCoalescingRule.SuppressedDiagnosticId)
				context.ReportSuppression(Suppression.Create(NullCoalescingRule, diagnostic));
			else if (diagnostic.Id == NullPropagationRule.SuppressedDiagnosticId)
				context.ReportSuppression(Suppression.Create(NullPropagationRule, diagnostic));
		}
	}
}
