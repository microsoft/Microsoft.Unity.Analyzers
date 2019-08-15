using System;
using System.Collections.Immutable;
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
	public class UnityObjectNullCoalescingAnalyzer : DiagnosticAnalyzer
	{
		public const string Id = "UNT0007";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			Id,
			title: Strings.UnityObjectNullCoalescingDiagnosticTitle,
			messageFormat: Strings.UnityObjectNullCoalescingDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.UnityObjectNullCoalescingDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeCoalesceExpression, SyntaxKind.CoalesceExpression);
		}

		private void AnalyzeCoalesceExpression(SyntaxNodeAnalysisContext context)
		{
			var binary = (BinaryExpressionSyntax)context.Node;
			var type = context.SemanticModel.GetTypeInfo(binary.Left);
			if (type.Type == null)
				return;

			if (!IsUnityObject(type.Type))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, binary.GetLocation(), binary.ToFullString()));
		}

		internal static bool IsUnityObject(ISymbol symbol)
		{
			var type = symbol as ITypeSymbol;
			if (type == null)
				return false;

			while (type != null)
			{
				if (type.ToDisplayString() == typeof(UnityEngine.Object).FullName)
					return true;

				type = type.BaseType;
			}

			return false;
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class UnityObjectNullCoalescingCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UnityObjectNullCoalescingAnalyzer.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			var coalescing = root.FindNode(context.Span) as BinaryExpressionSyntax;
			if (coalescing == null)
				return;

			// We do not want to fix expressions with possible side effects such as `Foo() ?? bar`
			// We could potentially rewrite by introducing a variable
			if (HasSideEffect(coalescing.Left))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.UnityObjectNullCoalescingCodeFixTitle,
					ct => ReplaceNullCoalescing(context.Document, coalescing, ct),
					coalescing.ToFullString()),
				context.Diagnostics);
		}

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

		private static async Task<Document> ReplaceNullCoalescing(Document document, BinaryExpressionSyntax coalescing, CancellationToken cancellationToken)
		{
			// obj ?? foo -> obj != null ? obj : foo
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var conditional = SyntaxFactory.ConditionalExpression(
				condition: SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, coalescing.Left, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
				whenTrue: coalescing.Left,
				whenFalse: coalescing.Right);

			return document.WithSyntaxRoot(root.ReplaceNode(coalescing, conditional));
		}
	}
}
