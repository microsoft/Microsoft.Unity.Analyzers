﻿/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Microsoft.Unity.Analyzers;

internal readonly struct ScriptInfo(ITypeSymbol symbol)
{
	internal static readonly Type[] Types =
	[
		typeof(UnityEngine.Networking.NetworkBehaviour),
		typeof(UnityEngine.StateMachineBehaviour),
		typeof(UnityEngine.EventSystems.UIBehaviour),
		typeof(UnityEditor.ScriptableWizard),
		typeof(UnityEditor.EditorWindow),
		typeof(UnityEditor.Editor),
		typeof(UnityEngine.ScriptableObject),
		typeof(UnityEngine.MonoBehaviour),
		typeof(UnityEditor.AssetPostprocessor),
		typeof(UnityEditor.AssetImporters.ScriptedImporter)
	];

	public bool HasMessages => Metadata != null;
	public Type? Metadata { get; } = GetMatchingMetadata(symbol);

	public static MethodInfo[] Messages { get; } = [.. Types.SelectMany(GetMessages)];

	public IEnumerable<MethodInfo> GetMessages()
	{
		if (Metadata == null)
			yield break;

		for (var type = Metadata; type != null && type != typeof(object); type = type.GetTypeInfo().BaseType)
		{
			foreach (var message in GetMessages(type))
				yield return message;
		}
	}

	public IEnumerable<MethodInfo> GetNotImplementedMessages(Accessibility? accessibility = null, ITypeSymbol? returnType = null)
	{
		foreach (var message in GetMessages())
		{
			if (IsImplemented(message))
				continue;

			if (accessibility.HasValue && !AccessibilityMatch(message, accessibility.Value))
				continue;

			if (returnType != null && !returnType.Matches(message.ReturnType))
				continue;

			yield return message;
		}
	}

	private static bool AccessibilityMatch(MethodInfo message, Accessibility accessibility)
	{
		return message switch
		{
			// If the message is declared as public or protected we need to honor it, other messages can be anything
			{ IsPublic: true, IsVirtual: true } => accessibility == Accessibility.Public,
			{ IsFamily: true, IsVirtual: true } => accessibility == Accessibility.Protected,
			_ => true
		};
	}

	private bool IsImplemented(MethodInfo method)
	{
		foreach (var member in symbol.GetMembers())
		{
			if (member is not IMethodSymbol methodSymbol)
				continue;

			if (methodSymbol.Matches(method))
				return true;
		}

		return false;
	}

	public bool IsMessage(IMethodSymbol method)
	{
		return GetMessages().Any(method.Matches);
	}

	private static Type? GetMatchingMetadata(ITypeSymbol symbol)
	{
		for (; symbol != null; symbol = symbol.BaseType)
		{
			if (symbol.BaseType == null)
				return null;

			var baseType = symbol.BaseType;

			foreach (var t in Types)
			{
				if (baseType.Matches(t))
					return t;
			}
		}

		return null;
	}

	private static IEnumerable<MethodInfo> GetMessages(Type type) => type.GetTypeInfo().DeclaredMethods;
}
