using System;
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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TagComparisonAnalyzer : DiagnosticAnalyzer
	{
		public const string Id = "UNT0002";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			Id,
			title: Strings.TagComparisonDiagnosticTitle,
			messageFormat: Strings.TagComparisonDiagnosticMessageFormat,
			category: DiagnosticCategory.Performance,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.TagComparisonDiagnosticDescription);

		public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public sealed override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.EqualsExpression);
		}

		private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
		{
			var binary = (BinaryExpressionSyntax)context.Node;

			if (IsTagComparison(binary.Left, binary.Right, context) || IsTagComparison(binary.Right, binary.Left, context))
				context.ReportDiagnostic(Diagnostic.Create(Rule, binary.GetLocation()));
		}

		private static bool IsTagIdentifier(SimpleNameSyntax id) => id.Identifier.Text == "tag";

		private static bool IsTagIdentifierAccess(SyntaxNode node)
		{
			switch (node)
			{
				case IdentifierNameSyntax id:
					if (IsTagIdentifier(id))
						return true;

					break;
				case MemberAccessExpressionSyntax ma:
					if (IsTagIdentifier(ma.Name))
						return true;

					break;
			}

			return false;
		}

		private static bool IsStringLiteral(SyntaxNode node) => node.IsKind(SyntaxKind.StringLiteralExpression);

		private bool IsTagComparison(SyntaxNode x, SyntaxNode y, SyntaxNodeAnalysisContext context)
		{
			if (!IsStringLiteral(y))
				return false;

			if (!IsTagIdentifierAccess(x))
				return false;

			var symbolInfo = context.SemanticModel.GetSymbolInfo(x);
			var property = symbolInfo.Symbol as IPropertySymbol;
			if (property == null)
				return false;

			return property.Name == "tag"
			       && property.ContainingType.Name == "Component"
			       && property.ContainingType.ContainingNamespace.Name == "UnityEngine";
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class TagComparisonCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TagComparisonAnalyzer.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			var comparison = root.FindNode(context.Span)
				.DescendantNodesAndSelf()
				.OfType<BinaryExpressionSyntax>()
				.FirstOrDefault();

			if (comparison == null)
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.TagComparisonCodeFixTitle,
					ct => ConvertToCompareTagAsync(context.Document, comparison, ct),
					comparison.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> ConvertToCompareTagAsync(Document document, BinaryExpressionSyntax comparison, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			var (literal, tagAccess) = comparison.Right.IsKind(SyntaxKind.StringLiteralExpression)
				? (comparison.Right, comparison.Left)
				: (comparison.Left, comparison.Right);

			var compareTag = (SyntaxNode)IdentifierName("CompareTag");

			switch (tagAccess)
			{
				case IdentifierNameSyntax _:
					break;
				case MemberAccessExpressionSyntax ma:
					compareTag = MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						ma.Expression,
						(SimpleNameSyntax)compareTag);
					break;
			}

			compareTag = InvocationExpression(
				(ExpressionSyntax)compareTag,
				ArgumentList(
					SeparatedList(new[] {Argument(literal)})));

			return document.WithSyntaxRoot(root.ReplaceNode(comparison, compareTag));
		}
	}
}
