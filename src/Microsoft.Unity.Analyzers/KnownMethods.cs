/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Unity.Analyzers
{
	internal static class KnownMethods
	{
		internal static readonly HashSet<string> GetComponentNames = new(new[]
{
			nameof(UnityEngine.Component.GetComponent),
			nameof(UnityEngine.Component.GetComponentInChildren),
			nameof(UnityEngine.Component.GetComponentInParent),

			nameof(UnityEngine.Component.GetComponents),
			nameof(UnityEngine.Component.GetComponentsInChildren),
			nameof(UnityEngine.Component.GetComponentsInParent),
		});

		public static bool IsGetComponentName(SimpleNameSyntax name) => GetComponentNames.Contains(name.Identifier.Text);

		public static bool IsGetComponent(IMethodSymbol method)
		{
			if (!GetComponentNames.Contains(method.Name))
				return false;

			var containingType = method.ContainingType;
			return containingType.Matches(typeof(UnityEngine.Component)) || containingType.Matches(typeof(UnityEngine.GameObject));
		}
	}
}
