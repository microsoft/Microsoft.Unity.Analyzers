/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Unity.Analyzers
{
	public abstract class BaseGetComponentAnalyzer : DiagnosticAnalyzer
	{
		protected internal static bool IsGenericGetComponent(InvocationExpressionSyntax invocation, SemanticModel model, [NotNullWhen(true)] out IMethodSymbol? method)
		{
			method = null;

			// We are looking for the exact GetComponent method, not other derivatives, so we do not want to use KnownMethods.IsGetComponentName(nameSyntax)
			if (invocation.GetMethodNameSyntax() is not { Identifier.Text: nameof(UnityEngine.Component.GetComponent) })
				return false;

			var symbol = ModelExtensions.GetSymbolInfo(model, invocation);
			if (symbol.Symbol is not IMethodSymbol methodSymbol)
				return false;

			method = methodSymbol;

			// We want Component.GetComponent or GameObject.GetComponent (given we already checked the exact name, we can use this one)
			if (!KnownMethods.IsGetComponent(method))
				return false;

			// We don't want arguments
			if (invocation.ArgumentList.Arguments.Count != 0)
				return false;

			// We want a type argument
			return method.TypeArguments.Length == 1;
		}

		protected static bool IsNonGenericGetComponent(InvocationExpressionSyntax invocation, SemanticModel model, [NotNullWhen(true)] out IMethodSymbol? method)
		{
			method = null;

			var symbol = ModelExtensions.GetSymbolInfo(model, invocation);
			if (symbol.Symbol is not IMethodSymbol methodSymbol)
				return false;

			method = methodSymbol;
			if (!KnownMethods.IsGetComponent(method))
				return false;

			return method.Parameters.Length != 0 && method.Parameters[0].Type.Matches(typeof(Type));
		}

		protected static bool HasInvalidTypeArgument(IMethodSymbol method, out string? identifier)
		{
			identifier = null;
			var argumentType = method.TypeArguments.FirstOrDefault();
			if (argumentType == null)
				return false;

			if (IsComponentOrInterface(argumentType))
				return false;

			if (argumentType.TypeKind == TypeKind.TypeParameter && argumentType is ITypeParameterSymbol typeParameter)
			{
				// We need to infer the effective generic type given usage, but we don't do that yet.
				if (typeParameter.ConstraintTypes.IsEmpty)
					return false;

				if (typeParameter.ConstraintTypes.Any(IsComponentOrInterface))
					return false;
			}

			identifier = argumentType.Name;
			return true;
		}

		protected static bool IsComponentOrInterface(ITypeSymbol argumentType)
		{
			return argumentType.Extends(typeof(UnityEngine.Component)) || argumentType.TypeKind == TypeKind.Interface;
		}

		protected static bool IsTryGetComponentSupported(SyntaxNodeAnalysisContext context)
		{
			// We need Unity 2019.2+ for proper support
			var goType = context.Compilation.GetTypeByMetadataName(typeof(UnityEngine.GameObject).FullName!);
			return goType?.MemberNames.Contains(nameof(UnityEngine.Component.TryGetComponent)) ?? false;
		}

		protected internal static bool TryGetConditionIdentifier(SyntaxNode ifNode, [NotNullWhen(true)] out SyntaxToken? conditionIdentifier, [NotNullWhen(true)] out BinaryExpressionSyntax? foundBinaryExpression, [NotNullWhen(true)] out IfStatementSyntax? foundIfStatement)
		{
			foundBinaryExpression = null;
			foundIfStatement = null;
			conditionIdentifier = null;

			if (ifNode is not IfStatementSyntax { Condition: BinaryExpressionSyntax binaryExpression } ifStatement)
				return false;

			foundBinaryExpression = binaryExpression;
			foundIfStatement = ifStatement;

			// We want an Equals/NotEquals condition
			if (!binaryExpression.IsKind(SyntaxKind.EqualsExpression) &&
				!binaryExpression.IsKind(SyntaxKind.NotEqualsExpression))
				return false;

			// We want IdentifierNameSyntax and null as operands
			conditionIdentifier = binaryExpression.Left switch
			{
				IdentifierNameSyntax leftIdentifierName when binaryExpression.Right is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } => leftIdentifierName.Identifier,
				LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } when binaryExpression.Right is IdentifierNameSyntax rightIdentifierName => rightIdentifierName.Identifier,
				_ => null
			};

			return conditionIdentifier.HasValue;
		}

		protected internal static bool TryGetTargetdentifier(SyntaxNode invocationParent, [NotNullWhen(true)] out SyntaxToken? targetIdentifier)
		{
			targetIdentifier = null;

			if (invocationParent is not EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator })
				return false;

			targetIdentifier = variableDeclarator.Identifier;
			return true;
		}

		protected internal static bool TryGetNextTopNode(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? nextNode)
		{
			nextNode = null;
			var topNode = node;
			while (topNode.Parent != null && topNode.Parent is not BlockSyntax)
				topNode = topNode.Parent;

			if (topNode.Parent is not BlockSyntax block)
				return false;

			var siblingsAndSelf = block.ChildNodes().ToImmutableArray();
			var invocationIndex = siblingsAndSelf.IndexOf(topNode);
			if (invocationIndex == -1)
				return false;

			var ifIndex = invocationIndex + 1;
			if (ifIndex >= siblingsAndSelf.Length)
				return false;

			nextNode = siblingsAndSelf[ifIndex];
			return true;
		}
	}
}
