/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
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

	private static bool IsOutRefCompatible(SemanticModel model, ExpressionSyntax expression)
	{
		return model.GetSymbolInfo(expression).Symbol is ILocalSymbol symbol
			   && IsOutRefCompatible(symbol.Type);
	}

	private static bool IsOutRefCompatible(SemanticModel model, TypeSyntax type)
	{
		return model.GetSymbolInfo(type).Symbol is ITypeSymbol symbol
			   && IsOutRefCompatible(symbol);
	}

	private static bool IsOutRefCompatible(ITypeSymbol type)
	{
		return type.Matches(typeof(Vector3))
			   || type.Matches(typeof(Quaternion));
	}

	public override bool TryGetArgumentExpression(SemanticModel model, ExpressionSyntax expression, [NotNullWhen(true)] out ArgumentSyntax? result)
	{
		result = null;

		if (expression is AssignmentExpressionSyntax assignment)
		{
			expression = assignment.Left;

			if (!IsOutRefCompatible(model, expression))
				return false;

			result = Argument(expression)
				.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))
				.WithLeadingTrivia(assignment.OperatorToken.TrailingTrivia);

			return true;
		}

		if (expression.FirstAncestorOrSelf<VariableDeclarationSyntax>() is not { } declaration)
			return false;

		if (!IsOutRefCompatible(model, declaration.Type))
			return false;

		var declarator = declaration.Variables.First();
		var type = declaration.Type;
		var typeString = type.ToString();

		var typeIdentifierName = type.IsVar ? IdentifierName(Identifier(TriviaList(), SyntaxKind.VarKeyword, typeString, typeString, TriviaList())) : IdentifierName(typeString);

		result = Argument(DeclarationExpression(typeIdentifierName, SingleVariableDesignation(Identifier(declarator.Identifier.Text))))
			.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))
			.WithLeadingTrivia(declarator.ParentTrivia);

		return true;
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
