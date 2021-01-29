/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnityObjectNullHandlingAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor NullCoalescingRule = new DiagnosticDescriptor(
			id: "UNT0007",
			title: Strings.UnityObjectNullCoalescingDiagnosticTitle,
			messageFormat: Strings.UnityObjectNullCoalescingDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.UnityObjectNullCoalescingDiagnosticDescription);

		internal static readonly DiagnosticDescriptor NullPropagationRule = new DiagnosticDescriptor(
			id: "UNT0008",
			title: Strings.UnityObjectNullPropagationDiagnosticTitle,
			messageFormat: Strings.UnityObjectNullPropagationDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.UnityObjectNullPropagationDiagnosticDescription);

		internal static readonly DiagnosticDescriptor CoalescingAssignmentRule = new DiagnosticDescriptor(
			id: "UNT0023",
			title: Strings.UnityObjectCoalescingAssignmentDiagnosticTitle,
			messageFormat: Strings.UnityObjectCoalescingAssignmentDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.UnityObjectCoalescingAssignmentDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NullCoalescingRule, NullPropagationRule, CoalescingAssignmentRule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeCoalesceExpression, SyntaxKind.CoalesceExpression);
			context.RegisterSyntaxNodeAction(AnalyzeConditionalAccessExpression, SyntaxKind.ConditionalAccessExpression);
			context.RegisterSyntaxNodeAction(AnalyzeCoalesceAssignmentExpression, SyntaxKind.CoalesceAssignmentExpression);
		}

		private static void AnalyzeCoalesceAssignmentExpression(SyntaxNodeAnalysisContext context)
		{
			var assignment = (AssignmentExpressionSyntax)context.Node;
			AnalyzeExpression(assignment, assignment.Left, context, CoalescingAssignmentRule);
		}

		private static void AnalyzeCoalesceExpression(SyntaxNodeAnalysisContext context)
		{
			var binary = (BinaryExpressionSyntax)context.Node;
			AnalyzeExpression(binary, binary.Left, context, NullCoalescingRule);
		}

		private static void AnalyzeConditionalAccessExpression(SyntaxNodeAnalysisContext context)
		{
			var access = (ConditionalAccessExpressionSyntax)context.Node;
			AnalyzeExpression(access, access.Expression, context, NullPropagationRule);
		}

		private static void AnalyzeExpression(ExpressionSyntax originalExpression, ExpressionSyntax typedExpression, SyntaxNodeAnalysisContext context, DiagnosticDescriptor rule)
		{
			var type = context.SemanticModel.GetTypeInfo(typedExpression);
			if (type.Type == null)
				return;

			if (!type.Type.Extends(typeof(UnityEngine.Object)))
				return;

			context.ReportDiagnostic(Diagnostic.Create(rule, originalExpression.GetLocation(), originalExpression.ToFullString()));
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class UnityObjectNullHandlingCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UnityObjectNullHandlingAnalyzer.NullCoalescingRule.Id, UnityObjectNullHandlingAnalyzer.CoalescingAssignmentRule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			var expression = root?
				.FindNode(context.Span).DescendantNodesAndSelf()
				.FirstOrDefault(c => c is BinaryExpressionSyntax || c is AssignmentExpressionSyntax || c is ConditionalAccessExpressionSyntax);

			CodeAction action;
			switch (expression)
			{
				// Null coalescing
				case BinaryExpressionSyntax bes:
					if (HasSideEffect(bes.Left))
						return;

					action = CodeAction.Create(Strings.UnityObjectNullCoalescingCodeFixTitle, ct => ReplaceNullCoalescingAsync(context.Document, bes, ct), bes.ToFullString());
					break;

				// Null propagation
				case ConditionalAccessExpressionSyntax caes:
					if (HasSideEffect(caes.Expression) && caes.WhenNotNull is MemberBindingExpressionSyntax)
						return;

					action = CodeAction.Create(Strings.UnityObjectNullPropagationCodeFixTitle, ct => ReplaceNullPropagationAsync(context.Document, caes, ct), caes.ToFullString());
					break;

				// Coalescing assignment
				case AssignmentExpressionSyntax aes:
					if (HasSideEffect(aes.Left))
						return;

					action = CodeAction.Create(Strings.UnityObjectCoalescingAssignmentCodeFixTitle, ct => ReplaceCoalescingAssignmentAsync(context.Document, aes, ct), aes.ToFullString());
					break;
				default:
					return;
			}

			context.RegisterCodeFix(action, context.Diagnostics);
		}

		// We do not want to fix expressions with possible side effects such as `Foo() ?? bar`
		// We could potentially rewrite by introducing a variable
		private static bool HasSideEffect(ExpressionSyntax expression)
		{
			switch (expression.Kind())
			{
				case SyntaxKind.SimpleMemberAccessExpression:
				case SyntaxKind.PointerMemberAccessExpression:
				case SyntaxKind.IdentifierName:
					return false;
			}

			return true;
		}

		private static async Task<Document> ReplaceWithAsync(Document document, SyntaxNode source, SyntaxNode replacement, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var newRoot = root.ReplaceNode(source, replacement);
			return newRoot == null ? document : document.WithSyntaxRoot(newRoot);
		}

		private static async Task<Document> ReplaceNullCoalescingAsync(Document document, BinaryExpressionSyntax coalescing, CancellationToken cancellationToken)
		{
			// obj ?? foo -> obj != null ? obj : foo
			var conditional = SyntaxFactory.ConditionalExpression(
				condition: SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, coalescing.Left, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
				whenTrue: coalescing.Left,
				whenFalse: coalescing.Right);

			return await ReplaceWithAsync(document, coalescing, conditional, cancellationToken);
		}

		private static async Task<Document> ReplaceCoalescingAssignmentAsync(Document document, AssignmentExpressionSyntax coalescing, CancellationToken cancellationToken)
		{
			// obj ??= foo -> obj = obj != null ? obj : foo
			var conditional = SyntaxFactory.ConditionalExpression(
				condition: SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, coalescing.Left, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
				whenTrue: coalescing.Left,
				whenFalse: coalescing.Right);

			var assignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, coalescing.Left, conditional);
			return await ReplaceWithAsync(document, coalescing, assignment, cancellationToken);
		}

		private static async Task<Document> ReplaceNullPropagationAsync(Document document, ConditionalAccessExpressionSyntax access, CancellationToken cancellationToken)
		{
			// obj?.member -> obj != null ? obj.member : null
			var mbes = (MemberBindingExpressionSyntax)access.WhenNotNull;

			var conditional = SyntaxFactory.ConditionalExpression(
				condition: SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, access.Expression, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
				whenTrue: SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, access.Expression, mbes.Name),
				whenFalse: SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

			return await ReplaceWithAsync(document, access, conditional, cancellationToken);
		}
	}

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnityObjectNullHandlingSuppressor : DiagnosticSuppressor
	{
		internal static readonly SuppressionDescriptor NullCoalescingRule = new SuppressionDescriptor(
			id: "USP0001",
			suppressedDiagnosticId: "IDE0029",
			justification: Strings.UnityObjectNullCoalescingSuppressorJustification);

		internal static readonly SuppressionDescriptor NullPropagationRule = new SuppressionDescriptor(
			id: "USP0002",
			suppressedDiagnosticId: "IDE0031",
			justification: Strings.UnityObjectNullPropagationSuppressorJustification);

		internal static readonly SuppressionDescriptor CoalescingAssignmentRule = new SuppressionDescriptor(
			id: "USP0017",
			suppressedDiagnosticId: "IDE0074",
			justification: Strings.UnityObjectCoalescingAssignmentSuppressorJustification);

		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(NullCoalescingRule, NullPropagationRule, CoalescingAssignmentRule);

		public override void ReportSuppressions(SuppressionAnalysisContext context)
		{
			foreach (var diagnostic in context.ReportedDiagnostics)
			{
				AnalyzeDiagnostic(diagnostic, context);
			}
		}

		private void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
		{
			var root = diagnostic.Location.SourceTree.GetRoot(context.CancellationToken);
			var node = root?.FindNode(diagnostic.Location.SourceSpan);

			// We can be tricked by extra parentheses for the condition, so go to the first concrete binary expression
			if (!(node?
				.DescendantNodesAndSelf()
				.OfType<BinaryExpressionSyntax>()
				.FirstOrDefault() is BinaryExpressionSyntax binaryExpression))
				return;

			AnalyzeBinaryExpression(diagnostic, context, binaryExpression);
		}

		private void AnalyzeBinaryExpression(Diagnostic diagnostic, SuppressionAnalysisContext context, BinaryExpressionSyntax binaryExpression)
		{
			switch (binaryExpression.Kind())
			{
				case SyntaxKind.EqualsExpression:
				case SyntaxKind.NotEqualsExpression:
					if (!binaryExpression.Right.IsKind(SyntaxKind.NullLiteralExpression))
						return;
					break;

				case SyntaxKind.CoalesceExpression:
					break;

				default:
					return;
			}

			var model = context.GetSemanticModel(binaryExpression.SyntaxTree);
			if (model == null)
				return;

			var type = model.GetTypeInfo(binaryExpression.Left);
			if (type.Type == null)
				return;

			if (!type.Type.Extends(typeof(UnityEngine.Object)))
				return;

			var rule = SupportedSuppressions.FirstOrDefault(r => r.SuppressedDiagnosticId == diagnostic.Id);
			if (rule != null)
				context.ReportSuppression(Suppression.Create(rule, diagnostic));
		}
	}
}
