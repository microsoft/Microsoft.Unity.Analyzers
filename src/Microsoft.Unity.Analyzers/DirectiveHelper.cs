/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Unity.Analyzers;

internal class DirectiveHelper
{
	public static bool IsInsideDirective(SyntaxNode node, string identifier)
	{
		var syntaxTree = node.SyntaxTree;
		var position = node.SpanStart;

		var root = syntaxTree.GetRoot();
		var directives = root.DescendantTrivia()
			.Where(t => t.SpanStart < position &&
					   (t.IsKind(SyntaxKind.IfDirectiveTrivia) ||
						t.IsKind(SyntaxKind.ElifDirectiveTrivia) ||
						t.IsKind(SyntaxKind.ElseDirectiveTrivia) ||
						t.IsKind(SyntaxKind.EndIfDirectiveTrivia)))
			.OrderBy(t => t.SpanStart)
			.ToList();

		var stack = new Stack<bool>();

		foreach (var directive in directives)
		{
			switch (directive.Kind())
			{
				case SyntaxKind.IfDirectiveTrivia:
					stack.Push(directive.GetStructure() is IfDirectiveTriviaSyntax ifDir && ContainsIdentifier(ifDir.Condition, identifier));
					break;
				case SyntaxKind.ElifDirectiveTrivia:
					if (stack.Count > 0)
						stack.Pop();

					stack.Push(directive.GetStructure() is ElifDirectiveTriviaSyntax elifDir && ContainsIdentifier(elifDir.Condition, identifier));
					break;
				case SyntaxKind.ElseDirectiveTrivia:
					if (stack.Count <= 0)
						continue;

					stack.Push(!stack.Pop());
					break;
				case SyntaxKind.EndIfDirectiveTrivia:
					if (stack.Count > 0)
						stack.Pop();
					break;
			}
		}

		return stack.Contains(true);
	}

	private static bool ContainsIdentifier(ExpressionSyntax? condition, string identifier)
	{
		return condition != null && EvaluateCondition(condition, identifier);
	}

	private static bool EvaluateCondition(ExpressionSyntax condition, string identifier)
	{
		return condition switch
		{
			IdentifierNameSyntax idName => idName.Identifier.Text == identifier,
			PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } prefixUnary => !EvaluateCondition(prefixUnary.Operand, identifier),
			ParenthesizedExpressionSyntax paren => EvaluateCondition(paren.Expression, identifier),
			BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalOrExpression } binary => EvaluateCondition(binary.Left, identifier) || EvaluateCondition(binary.Right, identifier),
			BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalAndExpression } binary => EvaluateCondition(binary.Left, identifier) && EvaluateCondition(binary.Right, identifier),
			_ => false
		};
	}

}
