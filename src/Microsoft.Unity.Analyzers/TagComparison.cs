﻿/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

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

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TagComparisonAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0002";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.TagComparisonDiagnosticTitle,
		messageFormat: Strings.TagComparisonDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.TagComparisonDiagnosticDescription);

	public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public sealed override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
		context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
	}

	private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
	{
		var expr = (InvocationExpressionSyntax)context.Node;
		var nameSyntax = expr.GetMethodNameSyntax();

		if (!IsSupportedMethod(context, nameSyntax))
			return;

		if (expr.Expression is MemberAccessExpressionSyntax mae)
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
		if (context.Node is not BinaryExpressionSyntax expr)
			return;

		if (IsReportableExpression(context, expr.Left)
			|| IsReportableExpression(context, expr.Right))
			context.ReportDiagnostic(Diagnostic.Create(Rule, expr.GetLocation()));
	}

	private static bool IsSupportedMethod(SyntaxNodeAnalysisContext context, SimpleNameSyntax? nameSyntax)
	{
		if (nameSyntax == null)
			return false;

		const string equalsName = "Equals";

		var name = GetMethodName(nameSyntax);
		if (name != equalsName)
			return false;

		var symbolInfo = context.SemanticModel.GetSymbolInfo(nameSyntax);
		if (symbolInfo.Symbol is not IMethodSymbol symbol)
			return false;

		if (symbol.Name != equalsName || symbol.Parameters.Length > 2)
			return false;

		if (!symbol.ReturnType.Matches(typeof(bool)))
			return false;

		var containgType = symbol.ContainingType;
		return containgType.Matches(typeof(string)) || containgType.Matches(typeof(object));
	}

	private static string GetMethodName(SimpleNameSyntax nameSyntax) => nameSyntax.Identifier.Text;

	private static bool IsReportableExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expr)
	{
		return IsReportableExpression(context.SemanticModel, expr);
	}

	internal static bool IsReportableExpression(SemanticModel model, ExpressionSyntax expr)
	{
		var symbol = model.GetSymbolInfo(expr).Symbol;
		if (symbol == null)
			return false;

		return IsReportableSymbol(symbol);
	}

	private static bool IsReportableSymbol(ISymbol symbol)
	{
		if (symbol is not IPropertySymbol propertySymbol)
			return false;

		var containingType = propertySymbol.ContainingType;
		return propertySymbol.Name == nameof(UnityEngine.Component.tag)
			   && (containingType.Matches(typeof(UnityEngine.GameObject)) || containingType.Matches(typeof(UnityEngine.Component)));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class TagComparisonCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => [TagComparisonAnalyzer.Rule.Id];

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var node = await context.GetFixableNodeAsync<SyntaxNode>(n => n is BinaryExpressionSyntax or InvocationExpressionSyntax);
		if (node == null)
			return;

		Func<CancellationToken, Task<Document>>? action = node switch
		{
			BinaryExpressionSyntax bes => ct => ReplaceBinaryExpressionAsync(context.Document, bes, ct),
			InvocationExpressionSyntax ies => ct => ReplaceInvocationExpressionAsync(context.Document, ies, ct),
			_ => null
		};

		if (action == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.TagComparisonCodeFixTitle,
				action,
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Document> ReplaceInvocationExpressionAsync(Document document, InvocationExpressionSyntax expr, CancellationToken ct)
	{
		var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
		var model = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
		if (model == null)
			return document;

		SortExpressions(model, expr, out var tagExpression, out var otherExpression);

		var replacement = BuildReplacementNode(tagExpression, otherExpression);
		var newRoot = root?.ReplaceNode(expr, replacement);
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}

	private static async Task<Document> ReplaceBinaryExpressionAsync(Document document, BinaryExpressionSyntax expr, CancellationToken ct)
	{
		var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
		var model = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
		if (model == null)
			return document;

		SortExpressions(model, expr, out var tagExpression, out var otherExpression);

		var replacement = BuildReplacementNode(tagExpression, otherExpression);

		if (expr.Kind() == SyntaxKind.NotEqualsExpression)
			replacement = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, replacement);

		var newRoot = root?.ReplaceNode(expr, replacement);
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
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

	private static void SortExpressions(SemanticModel model, InvocationExpressionSyntax expr, out ExpressionSyntax tagExpression, out ExpressionSyntax otherExpression)
	{
		if (expr is { Expression: MemberAccessExpressionSyntax mae, ArgumentList.Arguments.Count: 1 })
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
			.Select(argument => argument.Expression)
			.First();
	}

	private static ExpressionSyntax BuildReplacementNode(ExpressionSyntax tagExpression, ExpressionSyntax otherExpression)
	{
		var compareTagIdentifier = SyntaxFactory.IdentifierName(nameof(UnityEngine.Component.CompareTag));
		ExpressionSyntax invocation = compareTagIdentifier;
		if (tagExpression is MemberAccessExpressionSyntax mae)
		{
			invocation = SyntaxFactory.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				mae.Expression,
				compareTagIdentifier);
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
