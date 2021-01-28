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

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(NullCoalescingRule, NullPropagationRule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeCoalesceExpression, SyntaxKind.CoalesceExpression);
			context.RegisterSyntaxNodeAction(AnalyzeConditionalAccessExpression, SyntaxKind.ConditionalAccessExpression);
			context.RegisterSyntaxNodeAction(AnalyzeCoalesceAssignmentExpression, SyntaxKind.CoalesceAssignmentExpression);
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

		private static void AnalyzeCoalesceAssignmentExpression(SyntaxNodeAnalysisContext context)
		{
			var assign = (AssignmentExpressionSyntax)context.Node;
			AnalyzeExpression(assign, assign.Left, context, NullCoalescingRule);
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
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UnityObjectNullHandlingAnalyzer.NullCoalescingRule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			var node = root.FindNode(context.Span);

			if (!(root?.FindNode(context.Span) is BinaryExpressionSyntax || root?.FindNode(context.Span) is AssignmentExpressionSyntax))
				return;

			var coalescing = root?
				.FindNode(context.Span).DescendantNodesAndSelf()
				.FirstOrDefault(c => c is BinaryExpressionSyntax || c is AssignmentExpressionSyntax);
			
			if (coalescing == null)
				return;

				switch (coalescing)
				{
					case BinaryExpressionSyntax bes:
						if (HasSideEffect(bes.Left))
							return;
						context.RegisterCodeFix(
							CodeAction.Create(
								Strings.UnityObjectNullCoalescingCodeFixTitle,
								ct => ReplaceNullCoalescingAsync(context.Document, bes, ct),
								bes.ToFullString()),
							context.Diagnostics);
						break;
					case AssignmentExpressionSyntax aes:
						if (HasSideEffect(aes.Left))
							return;
						context.RegisterCodeFix(
								CodeAction.Create(
									Strings.UnityObjectNullCoalescingCodeFixTitle,
									ct => ReplaceNullCoalescingAssignmentAsync(context.Document, aes, ct),
									aes.ToFullString()),
								context.Diagnostics);
						break;
				}
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

		private static async Task<Document> ReplaceNullCoalescingAsync(Document document, BinaryExpressionSyntax coalescing, CancellationToken cancellationToken)
		{
			// obj ?? foo -> obj != null ? obj : foo
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var conditional = SyntaxFactory.ConditionalExpression(
				condition: SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, coalescing.Left, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
				whenTrue: coalescing.Left,
				whenFalse: coalescing.Right);
			var newRoot = root.ReplaceNode(coalescing, conditional);
			if (newRoot == null)
				return document;

			return document.WithSyntaxRoot(newRoot);
		}

		private static async Task<Document> ReplaceNullCoalescingAssignmentAsync(Document document, AssignmentExpressionSyntax coalescing, CancellationToken cancellationToken)
		{
			// obj ??= foo -> obj != null ? obj = obj : obj = foo
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			var conditional = SyntaxFactory.ConditionalExpression(
				condition: SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, coalescing.Left, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
				whenTrue: coalescing.Left.WithoutTrivia(),
				whenFalse: coalescing.Right);

			var assignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, coalescing.Left.WithoutTrailingTrivia(), conditional.WithoutLeadingTrivia());
			
			var newRoot = root.ReplaceNode(coalescing, assignment);
			if (newRoot == null)
				return document;

			return document.WithSyntaxRoot(newRoot);
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

		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(NullCoalescingRule, NullPropagationRule);

		public override void ReportSuppressions(SuppressionAnalysisContext context)
		{
			foreach (var diagnostic in context.ReportedDiagnostics)
			{
				AnalyzeDiagnostic(diagnostic, context);
			}
		}

		private static void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
		{
			var root = diagnostic.Location.SourceTree.GetRoot(context.CancellationToken);

			// We can be called in the context of a method argument or a regular expression
			if (!(root
				.FindNode(diagnostic.Location.SourceSpan)
				.DescendantNodesAndSelf()
				.OfType<ConditionalExpressionSyntax>()
				.FirstOrDefault() is ConditionalExpressionSyntax expressionSyntax))
				return;

			// We can be tricked by extra parentheses for the condition, so go to the first concrete binary expression
			if (!(expressionSyntax
				.Condition
				.DescendantNodesAndSelf()
				.OfType<BinaryExpressionSyntax>()
				.FirstOrDefault() is BinaryExpressionSyntax binaryExpression))
				return;

			AnalyzeBinaryExpression(diagnostic, context, binaryExpression);
		}

		private static void AnalyzeBinaryExpression(Diagnostic diagnostic, SuppressionAnalysisContext context, BinaryExpressionSyntax binaryExpression)
		{
			switch (binaryExpression.Kind())
			{
				case SyntaxKind.EqualsExpression:
				case SyntaxKind.NotEqualsExpression:
					break;
				default:
					return;
			}

			if (!binaryExpression.Right.IsKind(SyntaxKind.NullLiteralExpression))
				return;

			var model = context.GetSemanticModel(binaryExpression.SyntaxTree);
			if (model == null)
				return;

			var type = model.GetTypeInfo(binaryExpression.Left);
			if (type.Type == null)
				return;

			if (!type.Type.Extends(typeof(UnityEngine.Object)))
				return;

			if (diagnostic.Id == NullCoalescingRule.SuppressedDiagnosticId)
				context.ReportSuppression(Suppression.Create(NullCoalescingRule, diagnostic));
			else if (diagnostic.Id == NullPropagationRule.SuppressedDiagnosticId)
				context.ReportSuppression(Suppression.Create(NullPropagationRule, diagnostic));
		}
	}
}
