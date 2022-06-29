/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Unity.Analyzers;

internal static class SuppressionAnalysisContextExtensions
{
	public static T? GetSuppressibleNode<T>(this SuppressionAnalysisContext context, Diagnostic diagnostic) where T : SyntaxNode
	{
		return GetSuppressibleNode<T>(context, diagnostic, _ => true);
	}

	public static T? GetSuppressibleNode<T>(this SuppressionAnalysisContext context, Diagnostic diagnostic, Func<T, bool> predicate) where T : SyntaxNode
	{
		var location = diagnostic.Location;
		var sourceTree = location.SourceTree;
		var root = sourceTree?.GetRoot(context.CancellationToken);

		return root?
			.FindNode(location.SourceSpan)
			.DescendantNodesAndSelf()
			.OfType<T>()
			.FirstOrDefault(predicate);
	}
}
