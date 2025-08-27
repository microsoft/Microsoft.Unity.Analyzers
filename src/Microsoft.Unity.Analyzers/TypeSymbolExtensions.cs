/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Microsoft.Unity.Analyzers;

internal static class TypeSymbolExtensions
{
	public static bool Extends(this ITypeSymbol? symbol, Type? type)
	{
		if (symbol == null || type == null)
			return false;

		while (symbol != null)
		{
			if (symbol.Matches(type))
				return true;

			symbol = symbol.BaseType;
		}

		return false;
	}

	private static bool IsBuiltinAwaitableOf(INamedTypeSymbol typeSymbol, Type type)
	{
		return IsAwaitableOf(typeSymbol, type, typeof(UnityEngine.Awaitable));
	}

	private static bool IsUniTaskAwaitableOf(INamedTypeSymbol typeSymbol, Type type)
	{
		return IsAwaitableOf(typeSymbol, type, type == typeof(void) ? typeof(Cysharp.Threading.Tasks.UniTaskVoid) : typeof(Cysharp.Threading.Tasks.UniTask));
	}

	private static bool IsAwaitableOf(INamedTypeSymbol typeSymbol, Type _, Type awaiter)
	{
		// We do not want to use typeSymbol.Matches(awaiter) here, to prevent infinite recursion
		if (typeSymbol.Name != awaiter.Name)
			return false;

		return typeSymbol.ContainingNamespace.ToDisplayString() == awaiter.Namespace;
	}

	private static bool IsAwaitableOf(INamedTypeSymbol typeSymbol, Type type)
	{
		return IsBuiltinAwaitableOf(typeSymbol, type)
			   || IsUniTaskAwaitableOf(typeSymbol, type);
	}

	public static bool IsAwaitableOf(this ITypeSymbol symbol, Type type)
	{
		if (symbol is not INamedTypeSymbol named)
			return false;

		if (!IsAwaitableOf(named, type))
			return false;

		if (type == typeof(void))
			return named.TypeArguments.Length == 0;

		return named.TypeArguments.Length == 1 && Matches(named.TypeArguments[0], type);
	}

	public static bool Matches(this ITypeSymbol symbol, Type type)
	{
		if (type == typeof(UnityEngine.IEnumeratorOrVoid))
		{
			return (symbol.SpecialType is SpecialType.System_Void or SpecialType.System_Collections_IEnumerator)
				|| symbol.IsAwaitableOf(typeof(void))
				|| symbol.IsAwaitableOf(typeof(IEnumerator));
		}

		switch (symbol.SpecialType)
		{
			case SpecialType.System_Void:
				return type == typeof(void);
			case SpecialType.System_Boolean:
				return type == typeof(bool);
			case SpecialType.System_Int32:
				return type == typeof(int);
			case SpecialType.System_Single:
				return type == typeof(float);
		}

		if (type.IsArray)
		{
			return symbol is IArrayTypeSymbol array && Matches(array.ElementType, type.GetElementType()!);
		}

		if (symbol is not INamedTypeSymbol named)
			return false;

		if (type.IsConstructedGenericType)
		{
			var args = type.GetTypeInfo().GenericTypeArguments;
			if (args.Length != named.TypeArguments.Length)
				return false;

			for (var i = 0; i < args.Length; i++)
				if (!Matches(named.TypeArguments[i], args[i]))
					return false;

			return Matches(named.ConstructedFrom, type.GetGenericTypeDefinition());
		}

		if (symbol.IsAwaitableOf(type))
			return true;

		return named.Name == type.Name
			   && named.ContainingNamespace?.ToDisplayString() == type.Namespace;
	}
}
