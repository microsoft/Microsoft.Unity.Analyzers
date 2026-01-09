/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

public abstract class BasePositionAndRotationContext(string positionPropertyName, string rotationPropertyName, string positionAndRotationMethodName)
{
	public string PositionPropertyName { get; } = positionPropertyName;
	public string RotationPropertyName { get; } = rotationPropertyName;
	public string PositionAndRotationMethodName { get; } = positionAndRotationMethodName;

	public abstract bool TryGetPropertyExpression(SemanticModel model, ExpressionSyntax expression, [NotNullWhen(true)] out MemberAccessExpressionSyntax? result);
	public abstract bool TryGetArgumentExpression(SemanticModel model, ExpressionSyntax expression, [NotNullWhen(true)] out ArgumentSyntax? result);

	private bool IsPositionOrRotationCandidate(SemanticModel model, ExpressionSyntax expression)
	{
		var property = GetPropertyName(model, expression);
		if (property != PositionPropertyName && property != RotationPropertyName)
			return false;

		if (!TryGetPropertyExpression(model, expression, out var syntax))
			return false;

		if (!TryGetArgumentExpression(model, expression, out _))
			return false;

		var symbolInfo = model.GetSymbolInfo(syntax);
		if (symbolInfo.Symbol is not IPropertySymbol)
			return false;

		var expressionTypeInfo = model.GetTypeInfo(syntax.Expression);
		if (expressionTypeInfo.Type == null)
			return false;

		return expressionTypeInfo.Type.Extends(typeof(UnityEngine.Transform))
			|| expressionTypeInfo.Type.Extends(typeof(UnityEngine.Jobs.TransformAccess));
	}

	public virtual bool TryGetExpression(SyntaxNode node, [NotNullWhen(true)] out ExpressionSyntax? result)
	{
		if (node is ExpressionStatementSyntax statement)
			node = statement.Expression;

		result = node as AssignmentExpressionSyntax;
		return result != null;
	}

	public bool TryGetNextExpression(SemanticModel model, ExpressionSyntax expression, [NotNullWhen(true)] out ExpressionSyntax? nextExpression)
	{
		nextExpression = null;

		if (!IsPositionOrRotationCandidate(model, expression))
			return false;

		if (expression.FirstAncestorOrSelf<BlockSyntax>() == null)
			return false;

		if (expression.FirstAncestorOrSelf<StatementSyntax>() == null)
			return false;

		var block = expression.FirstAncestorOrSelf<BlockSyntax>();
		if (block == null)
			return false;

		var siblingsAndSelf = block.ChildNodes().ToImmutableArray();

		var candidate = expression.FirstAncestorOrSelf<StatementSyntax>();
		if (candidate == null)
			return false;

		var lastIndexOf = siblingsAndSelf.LastIndexOf(candidate);
		if (lastIndexOf == -1)
			return false;

		var nextIndex = lastIndexOf + 1;
		if (nextIndex == siblingsAndSelf.Length)
			return false;

		var statement = siblingsAndSelf[nextIndex];
		if (!TryGetExpression(statement, out var nextExpressionCandidate))
			return false;

		if (!IsPositionOrRotationCandidate(model, nextExpressionCandidate))
			return false;

		nextExpression = nextExpressionCandidate;
		return true;
	}

	public string GetPropertyName(SemanticModel model, ExpressionSyntax expression)
	{
		if (!TryGetPropertyExpression(model, expression, out var syntax))
			return string.Empty;

		return syntax.Name.ToString();
	}
}

public abstract class BasePositionAndRotationAnalyzer(BasePositionAndRotationContext expressionContext) : DiagnosticAnalyzer
{
	internal BasePositionAndRotationContext ExpressionContext { get; } = expressionContext;

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.SimpleAssignmentExpression);
	}

	protected void AnalyzeExpression(SyntaxNodeAnalysisContext context)
	{
		if (!ExpressionContext.TryGetExpression(context.Node, out var expression))
			return;

		var model = context.SemanticModel;
		if (!ExpressionContext.TryGetNextExpression(model, expression, out var nextExpression))
			return;

		if (!ExpressionContext.TryGetPropertyExpression(model, expression, out var syntax))
			return;

		if (!ExpressionContext.TryGetPropertyExpression(model, nextExpression, out var nextSyntax))
			return;

		if (!AreExpressionsEquivalent(model, syntax.Expression, nextSyntax.Expression))
			return;

		var property = ExpressionContext.GetPropertyName(model, expression);
		var nextProperty = ExpressionContext.GetPropertyName(model, nextExpression);
		if (property == nextProperty)
			return;

		// Check that the replacement method exists on target type in the current Unity version
		var type = model.GetTypeInfo(syntax.Expression).Type;
		if (type == null || !type.GetMembers().Any(m => m is IMethodSymbol && m.Name == ExpressionContext.PositionAndRotationMethodName))
			return;

		if (expression.FirstAncestorOrSelf<StatementSyntax>() is not { } statement)
			return;

		// Check for expression reuse that could change semantics when combining into a single method call
		if (DetectExpressionReuse(model, syntax, nextSyntax))
			return;

		OnPatternFound(context, statement);
	}

	private static bool AreExpressionsEquivalent(SemanticModel model, ExpressionSyntax expr1, ExpressionSyntax expr2)
	{
		// First try syntactic equivalence which handles cases like "transform" vs "this.transform"
		if (expr1.IsEquivalentTo(expr2))
			return true;

		// For member access expressions, we need to compare the entire chain semantically
		if (expr1 is MemberAccessExpressionSyntax member1 && expr2 is MemberAccessExpressionSyntax member2)
		{
			// Compare the member name symbols (e.g., "transform" property)
			var symbol1 = model.GetSymbolInfo(member1.Name).Symbol;
			var symbol2 = model.GetSymbolInfo(member2.Name).Symbol;

			if (symbol1 == null || symbol2 == null)
				return false;

			return SymbolEqualityComparer.Default.Equals(symbol1, symbol2) &&
				   AreExpressionsEquivalent(model, member1.Expression, member2.Expression);
		}

		// For other expressions, compare their symbols
		var exprSymbol1 = model.GetSymbolInfo(expr1).Symbol;
		var exprSymbol2 = model.GetSymbolInfo(expr2).Symbol;

		// If either symbol is null (including ambiguous cases with CandidateSymbols),
		// we conservatively treat them as not equivalent to avoid incorrect suggestions
		if (exprSymbol1 == null || exprSymbol2 == null)
			return false;

		return SymbolEqualityComparer.Default.Equals(exprSymbol1, exprSymbol2);
	}

	private static bool DetectExpressionReuse(SemanticModel model, MemberAccessExpressionSyntax candidate, MemberAccessExpressionSyntax expression)
	{
		var syntaxes = expression.Parent?.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
		return syntaxes != null && syntaxes.Any(syntax => AreExpressionsEquivalent(model, candidate, syntax));
	}

	protected abstract void OnPatternFound(SyntaxNodeAnalysisContext context, StatementSyntax statement);
}

public abstract class BasePositionAndRotationCodeFix(BasePositionAndRotationContext expressionContext) : CodeFixProvider
{
	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	private BasePositionAndRotationContext ExpressionContext { get; } = expressionContext;

	protected abstract string CodeFixTitle { get; }

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var statement = await context.GetFixableNodeAsync<StatementSyntax>();
		if (statement == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				CodeFixTitle,
				ct => ReplaceWithInvocationAsync(context.Document, statement, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private async Task<Document> ReplaceWithInvocationAsync(Document document, StatementSyntax statement, CancellationToken cancellationToken)
	{
		var model = await document.GetSemanticModelAsync(cancellationToken);
		if (model == null)
			return document;

		if (!ExpressionContext.TryGetExpression(statement, out var expression))
			return document;

		if (!ExpressionContext.TryGetNextExpression(model, expression, out var nextExpression))
			return document;

		if (nextExpression.FirstAncestorOrSelf<StatementSyntax>() is not { } nextStatement)
			return document;

		var property = ExpressionContext.GetPropertyName(model, expression);

		if (!ExpressionContext.TryGetArgumentExpression(model, expression, out var argument))
			return document;

		if (!ExpressionContext.TryGetArgumentExpression(model, nextExpression, out var nextArgument))
			return document;

		var arguments = new[] { argument, nextArgument };

		if (property != ExpressionContext.PositionPropertyName)
			Array.Reverse(arguments);

		var argList = ArgumentList()
			.AddArguments(arguments);

		if (!ExpressionContext.TryGetPropertyExpression(model, expression, out var syntax))
			return document;

		var invocationInstance = syntax.FirstAncestorOrSelf<MemberAccessExpressionSyntax>()?.Expression;
		if (invocationInstance == null)
			return document;

		var invocation = InvocationExpression(
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					invocationInstance,
					IdentifierName(ExpressionContext.PositionAndRotationMethodName)))
			.WithArgumentList(argList)
			.WithLeadingTrivia(statement.MergeLeadingTriviaWith(nextStatement));

		var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken);
		documentEditor.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
		documentEditor.ReplaceNode(nextStatement, ExpressionStatement(invocation));

		return documentEditor.GetChangedDocument();
	}
}
