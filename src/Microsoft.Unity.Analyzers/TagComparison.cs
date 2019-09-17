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
			context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
			context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
		}

		private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
		{
			var expr = (InvocationExpressionSyntax) context.Node;
			var nameSyntax = GetMethodNameSyntax(expr);

			if (!IsSupportedMethod(context, nameSyntax))
				return;

			var mae = expr.Expression as MemberAccessExpressionSyntax;
			if (mae != null)
			{
				if (IsReportableExpression(context, mae.Expression))
					context.ReportDiagnostic(Diagnostic.Create(Rule, expr.GetLocation()));
			}

			foreach (var argument in expr.ArgumentList.Arguments)
			{
				if (IsReportableExpression(context, argument.Expression))
					context.ReportDiagnostic(Diagnostic.Create(Rule, expr.GetLocation()));
			}
		}

		private static void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
		{
			var expr = context.Node as BinaryExpressionSyntax;
			if (expr == null)
				return;

			if (IsReportableExpression(context, expr.Left)
			    || IsReportableExpression(context, expr.Right))
				context.ReportDiagnostic(Diagnostic.Create(Rule, expr.GetLocation()));
		}

		private static bool IsSupportedMethod(SyntaxNodeAnalysisContext context, ExpressionSyntax nameSyntax)
		{
			if (nameSyntax == null)
				return false;

			var symbolInfo = context.SemanticModel.GetSymbolInfo(nameSyntax);
			var symbol = symbolInfo.Symbol as IMethodSymbol;
			if (symbol == null)
				return false;

			if (symbol.Name != "Equals" || symbol.Parameters.Length > 2)
				return false;

			if (!symbol.ReturnType.Matches(typeof(bool)))
				return false;

			var containgType = symbol.ContainingType;
			return containgType.Matches(typeof(string)) || containgType.Matches(typeof(object));
		}

		private static NameSyntax GetMethodNameSyntax(InvocationExpressionSyntax expr)
		{
			var nameSyntax = default(NameSyntax);

			var mae = expr.Expression as MemberAccessExpressionSyntax;
			if (mae != null)
				nameSyntax = mae.Name;

			var ies = expr.Expression as IdentifierNameSyntax;
			if (ies != null)
				nameSyntax = ies;

			return nameSyntax;
		}

		private static bool IsReportableExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expr)
		{
			return IsReportableExpression(context.SemanticModel, expr);
		}

		internal static bool IsReportableExpression(SemanticModel model, ExpressionSyntax expr)
		{
			var symbol = model.GetSymbolInfo(expr).Symbol;
			return IsReportableSymbol(symbol);
		}

		private static bool IsReportableSymbol(ISymbol symbol)
		{
			var propertySymbol = symbol as IPropertySymbol;
			if (propertySymbol == null)
				return false;

			var containingType = propertySymbol.ContainingType;
			return propertySymbol.Name == "tag" && (containingType.Matches(typeof(UnityEngine.GameObject)) || containingType.Matches(typeof(UnityEngine.Component)));
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

			var node = root.FindNode(context.Span)
				.DescendantNodesAndSelf().Where(n => n is BinaryExpressionSyntax || n is InvocationExpressionSyntax)
				.FirstOrDefault();

			if (node == null)
				return;

			Func<CancellationToken, Task<Document>> action = null;
			switch(node)
			{
				case BinaryExpressionSyntax bes:
					action = ct => ReplaceBinaryExpressionAsync(context.Document, bes, ct);
					break;
				case InvocationExpressionSyntax ies:
					action = ct => ReplaceInvocationExpressionAsync(context.Document, ies, ct);
					break;
			}

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.TagComparisonCodeFixTitle,
					action,
					node.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> ReplaceInvocationExpressionAsync(Document document, InvocationExpressionSyntax expr, CancellationToken ct)
		{
			var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
			var model = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);

			ExpressionSyntax tagExpression, otherExpression;
			SortExpressions(model, expr, out tagExpression, out otherExpression);

			var replacement = BuildReplacementNode(tagExpression, otherExpression);
			return document.WithSyntaxRoot(root.ReplaceNode(expr, replacement));
		}

		private static async Task<Document> ReplaceBinaryExpressionAsync(Document document, BinaryExpressionSyntax expr, CancellationToken ct)
		{
			var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
			var model = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);

			ExpressionSyntax tagExpression, otherExpression;
			SortExpressions(model, expr, out tagExpression, out otherExpression);

			var replacement = BuildReplacementNode(tagExpression, otherExpression);

			if (expr.Kind() == SyntaxKind.NotEqualsExpression)
				replacement = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, replacement);

			return document.WithSyntaxRoot(root.ReplaceNode(expr, replacement));
		}

		private static void SortExpressions(SemanticModel model, BinaryExpressionSyntax expr, out ExpressionSyntax tagExpression, out ExpressionSyntax otherExpression)
		{
			if (TagComparisonAnalyzer.IsReportableExpression(model, expr.Right))
			{
				tagExpression = expr.Right;
				otherExpression = expr.Left;
			}
			else
			{
				tagExpression = expr.Left;
				otherExpression = expr.Right;
			}
		}

		private static void SortExpressions(SemanticModel model, InvocationExpressionSyntax expr, out ExpressionSyntax tagExpression,
			out ExpressionSyntax otherExpression)
		{
			var mae = expr.Expression as MemberAccessExpressionSyntax;
			if (mae != null && expr.ArgumentList.Arguments.Count == 1)
			{
				if (TagComparisonAnalyzer.IsReportableExpression(model, mae.Expression))
				{
					tagExpression = mae.Expression;
					otherExpression = expr.ArgumentList.Arguments.First().Expression;
				}
				else
				{
					tagExpression = FindReportableExpression(model, expr.ArgumentList);
					otherExpression = mae.Expression;
				}
			}
			else
			{
				tagExpression = FindReportableExpression(model, expr.ArgumentList);
				// we cannot handle out parameter in lambda
				var expression = tagExpression;
				otherExpression = expr.ArgumentList.Arguments.First(a => !a.Expression.Equals(expression)).Expression;
			}
		}

		private static ExpressionSyntax FindReportableExpression(SemanticModel model, ArgumentListSyntax argumentList)
		{
			return argumentList
				.Arguments
				.Where(argument => TagComparisonAnalyzer.IsReportableExpression(model, argument.Expression))
				.Select(argument => argument.Expression).FirstOrDefault();
		}

		private static ExpressionSyntax BuildReplacementNode(ExpressionSyntax tagExpression, ExpressionSyntax otherExpression)
		{
			var CompareTagIdentifier = SyntaxFactory.IdentifierName("CompareTag");
			ExpressionSyntax invocation = CompareTagIdentifier;
			var mae = tagExpression as MemberAccessExpressionSyntax;
			if (mae != null)
			{
				invocation = SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					mae.Expression,
					CompareTagIdentifier);
			}

			var replacement = BuildInvocationExpression(invocation, otherExpression);
			return replacement;
		}

		private static ExpressionSyntax BuildInvocationExpression(ExpressionSyntax invocation, ExpressionSyntax argument)
		{
			return
				SyntaxFactory.InvocationExpression(invocation)
					.WithArgumentList(
						SyntaxFactory.ArgumentList(
							SyntaxFactory.SingletonSeparatedList(
								SyntaxFactory.Argument(argument))));
		}

	}
}
