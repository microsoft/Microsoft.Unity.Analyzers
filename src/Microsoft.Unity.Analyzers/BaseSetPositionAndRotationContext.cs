/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

public class BaseSetPositionAndRotationContext : BasePositionAndRotationContext
{
	protected BaseSetPositionAndRotationContext(string positionPropertyName, string rotationPropertyName, string positionAndRotationMethodName) : base(positionPropertyName, rotationPropertyName, positionAndRotationMethodName)
	{
	}

	public override bool TryGetPropertyExpression(SemanticModel model, ExpressionSyntax expression, [NotNullWhen(true)] out MemberAccessExpressionSyntax? result)
	{
		result = null;

		if (expression is not AssignmentExpressionSyntax assignment)
			return false;

		result = assignment.Left as MemberAccessExpressionSyntax;
		return result != null;
	}

	public override ArgumentSyntax? TryGetArgumentExpression(SemanticModel model, ExpressionSyntax expression)
	{
		if (expression is not AssignmentExpressionSyntax assignment)
			return null;

		return Argument(assignment.Right)
			.WithLeadingTrivia(assignment.OperatorToken.TrailingTrivia);
	}
}
