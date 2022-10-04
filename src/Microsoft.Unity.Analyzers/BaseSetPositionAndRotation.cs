/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

public class BaseSetPositionAndRotationContext
{
	public string PositionPropertyName { get; }
	public string RotationPropertyName { get; }
	public string SetPositionAndRotationMethodName { get; }

	protected BaseSetPositionAndRotationContext(string positionPropertyName, string rotationPropertyName, string setPositionAndRotationMethodName)
	{
		PositionPropertyName = positionPropertyName;
		RotationPropertyName = rotationPropertyName;
		SetPositionAndRotationMethodName = setPositionAndRotationMethodName;
	}

	private bool IsSetPositionOrRotation(SemanticModel model, AssignmentExpressionSyntax assignmentExpression)
	{
		var property = GetProperty(assignmentExpression);
		if (property != PositionPropertyName && property != RotationPropertyName)
			return false;

		if (assignmentExpression.Left is not MemberAccessExpressionSyntax left)
			return false;

		var leftSymbol = model.GetSymbolInfo(left);
		if (leftSymbol.Symbol is not IPropertySymbol)
			return false;
		
		var leftExpressionTypeInfo = model.GetTypeInfo(left.Expression);
		if (leftExpressionTypeInfo.Type == null)
			return false;

		return leftExpressionTypeInfo.Type.Extends(typeof(UnityEngine.Transform));
	}

	public bool GetNextAssignmentExpression(SemanticModel model, AssignmentExpressionSyntax assignmentExpression, [NotNullWhen(true)] out AssignmentExpressionSyntax? assignmentExpressionSyntax)
	{
		assignmentExpressionSyntax = null;

		if (!IsSetPositionOrRotation(model, assignmentExpression))
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

		if (!IsSetPositionOrRotation(model, nextAssignmentExpression))
			return false;

		assignmentExpressionSyntax = nextAssignmentExpression;
		return true;
	}

	public static string GetProperty(AssignmentExpressionSyntax assignmentExpression)
	{
		if (assignmentExpression.Left is not MemberAccessExpressionSyntax left)
			return string.Empty;

		return left.Name.ToString();
	}
}

public abstract class BaseSetPositionAndRotationAnalyzer : DiagnosticAnalyzer
{
	private BaseSetPositionAndRotationContext ExpressionContext { get; }

	protected BaseSetPositionAndRotationAnalyzer(BaseSetPositionAndRotationContext expressionContext)
	{
		ExpressionContext = expressionContext;
	}

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
	}

	private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not AssignmentExpressionSyntax assignmentExpression)
			return;

		if (!ExpressionContext.GetNextAssignmentExpression(context.SemanticModel, assignmentExpression, out var nextAssignmentExpression))
			return;

		// We know that both assignmentExpression.Left and nextAssignmentExpression.Left are MemberAccessExpressionSyntax
		// cf. GetNextAssignmentExpression calling IsSetPositionOrRotation
		var left = (MemberAccessExpressionSyntax)assignmentExpression.Left;
		var nextLeft = (MemberAccessExpressionSyntax)nextAssignmentExpression.Left;
		
		if (left.Expression.ToString() != nextLeft.Expression.ToString())
			return;

		var property = BaseSetPositionAndRotationContext.GetProperty(assignmentExpression);
		var nextProperty = BaseSetPositionAndRotationContext.GetProperty(nextAssignmentExpression);
		if (property == nextProperty)
			return;

		// Check that the replacement method exists on Transform in the current Unity version
		var model = context.SemanticModel;
		var type = model.GetTypeInfo(left.Expression).Type;
		if (type == null || !type.GetMembers().Any(m => m is IMethodSymbol && m.Name == ExpressionContext.SetPositionAndRotationMethodName))
			return;

		OnPatternFound(context, assignmentExpression);
	}

	protected abstract void OnPatternFound(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignmentExpressionSyntax);
}

public abstract class BaseSetPositionAndRotationCodeFix : CodeFixProvider
{
	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	private BaseSetPositionAndRotationContext ExpressionContext { get; }

	protected BaseSetPositionAndRotationCodeFix(BaseSetPositionAndRotationContext expressionContext)
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

		var property = BaseSetPositionAndRotationContext.GetProperty(assignmentExpression);
		var arguments = new[]
		{
			Argument(assignmentExpression.Right)
				.WithLeadingTrivia(assignmentExpression.OperatorToken.TrailingTrivia),
			Argument(nextAssignmentExpression.Right)
				.WithLeadingTrivia(nextAssignmentExpression.OperatorToken.TrailingTrivia)
		};

		if (property != ExpressionContext.PositionPropertyName)
			Array.Reverse(arguments);

		var argList = ArgumentList()
			.AddArguments(arguments);

		var baseExpression = assignmentExpression
			.Left
			.FirstAncestorOrSelf<MemberAccessExpressionSyntax>()?.Expression;

		if (baseExpression == null)
			return document;

		var invocation = InvocationExpression(
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					baseExpression,
					IdentifierName(ExpressionContext.SetPositionAndRotationMethodName)))
			.WithArgumentList(argList)
			.WithLeadingTrivia(assignmentExpression.MergeLeadingTriviaWith(nextAssignmentExpression));

		var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken);
		documentEditor.RemoveNode(assignmentExpression.Parent, SyntaxRemoveOptions.KeepNoTrivia);
		documentEditor.ReplaceNode(nextAssignmentExpression, invocation);

		return documentEditor.GetChangedDocument();
	}
}
