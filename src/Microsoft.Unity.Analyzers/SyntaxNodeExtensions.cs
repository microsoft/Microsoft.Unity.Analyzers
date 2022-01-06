/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Microsoft.CodeAnalysis;

namespace Microsoft.Unity.Analyzers
{
	internal static class SyntaxNodeExtensions
	{
		public static SyntaxTriviaList MergeLeadingTriviaWith(this SyntaxNode first, SyntaxNode second)
		{
			var merged = first.GetLeadingTrivia().AddRange(second.GetLeadingTrivia());
			var result = SyntaxTriviaList.Empty;

			SyntaxTrivia previous = new SyntaxTrivia();
			foreach (var trivia in merged)
			{
				if (!trivia.IsEquivalentTo(previous))
					result = result.Add(trivia);

				previous = trivia;
			}

			return result;
		}
	}
}
