/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Unity.Analyzers;

internal static class SyntaxNodeExtensions
{
	private static readonly string[] EditorOnlyMessages = ["OnValidate", "Reset"];

	extension(SyntaxNode node)
	{
		public bool IsInsideEditorOnlyMessage(SemanticModel semanticModel)
		{
			var methodSyntax = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
			if (methodSyntax == null)
				return false;

			var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax);
			if (methodSymbol == null)
				return false;

			var scriptInfo = new ScriptInfo(methodSymbol.ContainingType);
			if (!scriptInfo.HasMessages)
				return false;

			return EditorOnlyMessages.Contains(methodSymbol.Name) && scriptInfo.IsMessage(methodSymbol);
		}
	}

	extension(SyntaxNode first)
	{
		public SyntaxTriviaList MergeLeadingTriviaWith(SyntaxNode second)
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
