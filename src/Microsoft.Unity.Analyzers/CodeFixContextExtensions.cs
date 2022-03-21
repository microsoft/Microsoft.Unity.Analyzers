/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.Unity.Analyzers;

internal static class CodeFixContextExtensions
{
	public static async Task<T?> GetFixableNodeAsync<T>(this CodeFixContext context) where T : SyntaxNode
	{
		return await GetFixableNodeAsync<T>(context, _ => true);
	}

	public static async Task<T?> GetFixableNodeAsync<T>(this CodeFixContext context, Func<T, bool> predicate) where T : SyntaxNode
	{
		var root = await context
			.Document
			.GetSyntaxRootAsync(context.CancellationToken)
			.ConfigureAwait(false);

		return root?
			.FindNode(context.Span)
			.DescendantNodesAndSelf()
			.OfType<T>()
			.FirstOrDefault(predicate);
	}
}
