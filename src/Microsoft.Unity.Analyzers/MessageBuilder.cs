/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.Unity.Analyzers;

internal readonly struct MessageBuilder(SyntaxGenerator generator)
{
	public SyntaxNode[] CreateParameters(MethodInfo message)
	{
		var parameters = message.GetParameters();
		var nodes = new SyntaxNode[parameters.Length];

		for (int i = 0; i < parameters.Length; i++)
		{
			var parameter = parameters[i];
			nodes[i] = generator.ParameterDeclaration(name: parameter.Name, type: CreateTypeReference(parameter.ParameterType));
		}

		return nodes;
	}

	private SyntaxNode CreateTypeReference(Type type)
	{
		if (type == typeof(void) || type == typeof(UnityEngine.IEnumeratorOrVoid))
			return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));

		if (type.IsArray)
			return generator.ArrayTypeExpression(CreateTypeReference(type.GetElementType()!));

		if (type.IsConstructedGenericType)
			return generator.WithTypeArguments(
				CreateTypeReference(type.GetGenericTypeDefinition()),
				type.GetGenericArguments().Select(CreateTypeReference));

		if (type.IsPrimitive)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
					return generator.TypeExpression(SpecialType.System_Boolean);
				case TypeCode.Int32:
					return generator.TypeExpression(SpecialType.System_Int32);
				case TypeCode.Single:
					return generator.TypeExpression(SpecialType.System_Single);
			}
		}

		return SyntaxFactory.ParseTypeName(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", type.Namespace, TypeName(type)));
	}

	private static string TypeName(Type type)
	{
		var name = type.Name;
		var index = name.IndexOf("`", StringComparison.Ordinal);
		if (index > 0)
			name = name.Substring(0, index);

		return name;
	}
}
