/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.Unity.Analyzers;

internal static class CodeFixContextExtensions
{
	public static Task<T?> GetFixableNodeAsync<T>(this CodeFixContext context) where T : SyntaxNode
	{
		return GetFixableNodeAsync<T>(context, _ => true);
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

	public static async Task<bool> IsReferencedAsync(this CodeFixContext context, SyntaxNode declaration)
	{
		var semanticModel = await context.Document
			.GetSemanticModelAsync(context.CancellationToken)
			.ConfigureAwait(false);

		var symbol = semanticModel?.GetDeclaredSymbol(declaration);
		if (symbol == null)
			return false;

		var references = await SymbolFinder.FindReferencesAsync(symbol, context.Document.Project.Solution, context.CancellationToken);
		return references.Any(r => r.Locations.Any());
	}
}
