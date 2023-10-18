/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

public class BaseGetPositionAndRotationContext : BasePositionAndRotationContext
{
	protected BaseGetPositionAndRotationContext(string positionPropertyName, string rotationPropertyName, string positionAndRotationMethodName) : base(positionPropertyName, rotationPropertyName, positionAndRotationMethodName)
	{
	}

	public override bool TryGetPropertyExpression(SemanticModel model, ExpressionSyntax expression, [NotNullWhen(true)] out MemberAccessExpressionSyntax? result)
	{
		if (expression is AssignmentExpressionSyntax assignment)
			result = assignment.Right as MemberAccessExpressionSyntax;
		else
			result = expression as MemberAccessExpressionSyntax;

		return result != null;
	}

	public override ArgumentSyntax? TryGetArgumentExpression(SemanticModel model, ExpressionSyntax expression)
	{
		if (expression is AssignmentExpressionSyntax assignment)
		{
			expression = assignment.Left;

			// We need to check for compatibility here with 'out' keyword
			if (model.GetSymbolInfo(expression).Symbol is not ILocalSymbol)
				return null;

			return Argument(expression)
				.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))
				.WithLeadingTrivia(assignment.OperatorToken.TrailingTrivia);
		}

		if (expression.FirstAncestorOrSelf<VariableDeclarationSyntax>() is not { } declaration)
			return null;

		var declarator = declaration.Variables.First();
		var type = declaration.Type;
		var typeString = type.ToString();

		var typeIdentifierName = type.IsVar ? IdentifierName(Identifier(TriviaList(), SyntaxKind.VarKeyword, typeString, typeString, TriviaList())) : IdentifierName(typeString);

		return Argument(DeclarationExpression(typeIdentifierName, SingleVariableDesignation(Identifier(declarator.Identifier.Text))))
			.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))
			.WithLeadingTrivia(declarator.ParentTrivia);
	}

	public override bool TryGetExpression(SyntaxNode node, [NotNullWhen(true)] out ExpressionSyntax? result)
	{
		if (node is LocalDeclarationStatementSyntax statement)
			node = statement.Declaration;

		if (node is not VariableDeclarationSyntax declaration)
			return base.TryGetExpression(node, out result);

		var variables = declaration.Variables;
		result = variables.Count != 1 ? null : variables.First().Initializer?.Value;
		return result != null;
	}

}
