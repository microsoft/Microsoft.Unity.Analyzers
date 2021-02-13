/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.Unity.Analyzers
{
	internal static class KnownMethods
	{
		internal static readonly HashSet<string> GetComponentNames = new HashSet<string>(new[]
{
			nameof(UnityEngine.Component.GetComponent),
			nameof(UnityEngine.Component.GetComponentInChildren),
			nameof(UnityEngine.Component.GetComponentInParent),

			nameof(UnityEngine.Component.GetComponents),
			nameof(UnityEngine.Component.GetComponentsInChildren),
			nameof(UnityEngine.Component.GetComponentsInParent),
		});

		public static bool IsGetComponent(IMethodSymbol method)
		{
			if (!GetComponentNames.Contains(method.Name))
				return false;

			var containingType = method.ContainingType;
			return containingType.Matches(typeof(UnityEngine.Component)) || containingType.Matches(typeof(UnityEngine.GameObject));
		}
	}
}
