/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

internal static class CachedStringIdField
{
	public static string? GetOrCreate(DocumentEditor editor, TypeDeclarationSyntax declaration,
		IMethodSymbol factory, string value, string suffix, IReadOnlyList<ExpressionSyntax> usages, CancellationToken cancellationToken)
	{
		var model = editor.SemanticModel;
		if (model.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type)
			return null;

		foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
		{
			if (!field.IsStatic || !field.IsReadOnly || field.Type.SpecialType != SpecialType.System_Int32)
				continue;

			foreach (var reference in field.DeclaringSyntaxReferences)
			{
				if (reference.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax
					{
						Initializer.Value: InvocationExpressionSyntax initializer
					}
					|| initializer.ArgumentList.Arguments.Count != 1)
					continue;

				var fieldModel = initializer.SyntaxTree == model.SyntaxTree
					? model
					: model.Compilation.GetSemanticModel(initializer.SyntaxTree);

				if (SymbolEqualityComparer.Default.Equals(fieldModel.GetSymbolInfo(initializer, cancellationToken).Symbol, factory)
					&& fieldModel.GetConstantValue(initializer.ArgumentList.Arguments[0].Expression, cancellationToken).Value is string existingValue
					&& existingValue == value
					&& CanReference(model, usages, field.Name, field))
					return field.Name;
			}
		}

		var baseName = GenerateFieldName(value, suffix);
		var fieldName = baseName;
		var counter = 1;
		while (fieldName == type.Name || type.GetMembers(fieldName).Length > 0 || !CanReference(model, usages, fieldName))
			fieldName = baseName + counter++;

		ExpressionSyntax factoryExpression = IdentifierName(factory.Name);
		var visibleFactories = model.LookupSymbols(declaration.OpenBraceToken.Span.End, name: factory.Name);
		if (visibleFactories.Length != 1 || !SymbolEqualityComparer.Default.Equals(visibleFactories[0], factory))
		{
			var factoryType = ParseName(factory.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
				.WithAdditionalAnnotations(Simplifier.Annotation);
			factoryExpression = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, factoryType, IdentifierName(factory.Name));
		}

		var initializerExpression = InvocationExpression(
			factoryExpression,
			ArgumentList(SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value))))));

		var fieldDeclaration = FieldDeclaration(
				VariableDeclaration(
					PredefinedType(Token(SyntaxKind.IntKeyword)),
					SingletonSeparatedList(
						VariableDeclarator(Identifier(fieldName))
							.WithInitializer(EqualsValueClause(initializerExpression)))))
			.AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword));

		editor.InsertMembers(declaration, 0, [fieldDeclaration]);
		return fieldName;
	}

	private static bool CanReference(SemanticModel model, IReadOnlyList<ExpressionSyntax> usages, string name, IFieldSymbol? existingField = null)
	{
		foreach (var usage in usages)
		{
			var symbols = model.LookupSymbols(usage.SpanStart, name: name);
			if (existingField == null ? symbols.Length != 0
				: symbols.Length != 1 || !SymbolEqualityComparer.Default.Equals(symbols[0], existingField))
				return false;
		}

		return true;
	}

	private static string GenerateFieldName(string value, string suffix)
	{
		var cleaned = Regex.Replace(value, @"[^a-zA-Z0-9]", " ");
		var words = cleaned.Split([' '], StringSplitOptions.RemoveEmptyEntries);
		var name = string.Concat(words.Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));

		if (name.Length > 0 && char.IsDigit(name[0]))
			name = "_" + name;

		return name + suffix;
	}
}
