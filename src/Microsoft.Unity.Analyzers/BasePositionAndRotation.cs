/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
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

public class BasePositionAndRotationContext
{
	public string PositionPropertyName { get; }
	public string RotationPropertyName { get; }
	public string PositionAndRotationMethodName { get; }

	protected BasePositionAndRotationContext(string positionPropertyName, string rotationPropertyName, string positionAndRotationMethodName)
	{
		PositionPropertyName = positionPropertyName;
		RotationPropertyName = rotationPropertyName;
		PositionAndRotationMethodName = positionAndRotationMethodName;
	}

	public MemberAccessExpressionSyntax? TryGetPropertyExpression(AssignmentExpressionSyntax assignmentExpression)
	{
		return assignmentExpression.Left as MemberAccessExpressionSyntax;
	}

	public ArgumentSyntax GetArgumentExpression(AssignmentExpressionSyntax assignmentExpression)
	{
		return Argument(assignmentExpression.Right);
	}

	private bool IsPositionOrRotationCandidate(SemanticModel model, AssignmentExpressionSyntax assignmentExpression)
	{
		var property = GetPropertyName(assignmentExpression);
		if (property != PositionPropertyName && property != RotationPropertyName)
			return false;

		if (TryGetPropertyExpression(assignmentExpression) is not { } syntax)
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

	public bool GetNextAssignmentExpression(SemanticModel model, AssignmentExpressionSyntax assignmentExpression, [NotNullWhen(true)] out AssignmentExpressionSyntax? assignmentExpressionSyntax)
	{
		assignmentExpressionSyntax = null;

		if (!IsPositionOrRotationCandidate(model, assignmentExpression))
			return false;

		if (assignmentExpression.FirstAncestorOrSelf<BlockSyntax>() == null)
			return false;

		if (assignmentExpression.FirstAncestorOrSelf<ExpressionStatementSyntax>() == null)
			return false;

		var block = assignmentExpression.FirstAncestorOrSelf<BlockSyntax>();
		if (block == null)
			return false;

		var siblingsAndSelf = block.ChildNodes().ToImmutableArray();
		var expression = assignmentExpression.FirstAncestorOrSelf<ExpressionStatementSyntax>();
		if (expression == null)
			return false;

		var lastIndexOf = siblingsAndSelf.LastIndexOf(expression);
		if (lastIndexOf == -1)
			return false;

		var nextIndex = lastIndexOf + 1;
		if (nextIndex == siblingsAndSelf.Length)
			return false;

		var statement = siblingsAndSelf[nextIndex];
		if (statement is not ExpressionStatementSyntax {Expression: AssignmentExpressionSyntax nextAssignmentExpression})
			return false;

		if (!IsPositionOrRotationCandidate(model, nextAssignmentExpression))
			return false;

		assignmentExpressionSyntax = nextAssignmentExpression;
		return true;
	}

	public string GetPropertyName(AssignmentExpressionSyntax assignmentExpression)
	{
		if (TryGetPropertyExpression(assignmentExpression) is not { } syntax)
			return string.Empty;

		return syntax.Name.ToString();
	}
}

public abstract class BasePositionAndRotationAnalyzer : DiagnosticAnalyzer
{
	internal BasePositionAndRotationContext ExpressionContext { get; }

	protected BasePositionAndRotationAnalyzer(BasePositionAndRotationContext expressionContext)
	{
		ExpressionContext = expressionContext;
	}

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
		context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.VariableDeclaration);
	}

	private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not AssignmentExpressionSyntax assignmentExpression)
			return;

		if (!ExpressionContext.GetNextAssignmentExpression(context.SemanticModel, assignmentExpression, out var nextAssignmentExpression))
			return;

		if (ExpressionContext.TryGetPropertyExpression(assignmentExpression) is not { } syntax)
			return;

		if (ExpressionContext.TryGetPropertyExpression(nextAssignmentExpression) is not { } nextSyntax)
			return;
		
		if (syntax.Expression.ToString() != nextSyntax.Expression.ToString())
			return;

		var property = ExpressionContext.GetPropertyName(assignmentExpression);
		var nextProperty = ExpressionContext.GetPropertyName(nextAssignmentExpression);
		if (property == nextProperty)
			return;

		// Check that the replacement method exists on target type in the current Unity version
		var model = context.SemanticModel;
		var type = model.GetTypeInfo(syntax.Expression).Type;
		if (type == null || !type.GetMembers().Any(m => m is IMethodSymbol && m.Name == ExpressionContext.PositionAndRotationMethodName))
			return;

		OnPatternFound(context, assignmentExpression);
	}

	protected abstract void OnPatternFound(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignmentExpressionSyntax);
}

public abstract class BasePositionAndRotationCodeFix : CodeFixProvider
{
	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	private BasePositionAndRotationContext ExpressionContext { get; }

	protected BasePositionAndRotationCodeFix(BasePositionAndRotationContext expressionContext)
	{
		ExpressionContext = expressionContext;
	}

	protected abstract string CodeFixTitle { get;}
	
	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var expression = await context.GetFixableNodeAsync<AssignmentExpressionSyntax>();
		if (expression == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				CodeFixTitle,
				ct => ReplaceWithInvocationAsync(context.Document, expression, ct),
				expression.ToFullString()),
			context.Diagnostics);
	}

	private async Task<Document> ReplaceWithInvocationAsync(Document document, AssignmentExpressionSyntax assignmentExpression, CancellationToken cancellationToken)
	{
		var model = await document.GetSemanticModelAsync(cancellationToken);
		if (model == null)
			return document;

		if (!ExpressionContext.GetNextAssignmentExpression(model, assignmentExpression, out var nextAssignmentExpression))
			return document;

		var property = ExpressionContext.GetPropertyName(assignmentExpression);
		var arguments = new[]
		{
			ExpressionContext
				.GetArgumentExpression(assignmentExpression)
				.WithLeadingTrivia(assignmentExpression.OperatorToken.TrailingTrivia),
			ExpressionContext
				.GetArgumentExpression(nextAssignmentExpression)
				.WithLeadingTrivia(nextAssignmentExpression.OperatorToken.TrailingTrivia)
		};

		if (property != ExpressionContext.PositionPropertyName)
			Array.Reverse(arguments);

		var argList = ArgumentList()
			.AddArguments(arguments);

		var baseExpression = ExpressionContext.TryGetPropertyExpression(assignmentExpression)?
			.FirstAncestorOrSelf<MemberAccessExpressionSyntax>()?.Expression;

		if (baseExpression == null)
			return document;

		var invocation = InvocationExpression(
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					baseExpression,
					IdentifierName(ExpressionContext.PositionAndRotationMethodName)))
			.WithArgumentList(argList)
			.WithLeadingTrivia(assignmentExpression.MergeLeadingTriviaWith(nextAssignmentExpression));

		var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken);
		documentEditor.RemoveNode(assignmentExpression.Parent, SyntaxRemoveOptions.KeepNoTrivia);
		documentEditor.ReplaceNode(nextAssignmentExpression, invocation);

		return documentEditor.GetChangedDocument();
	}
}
