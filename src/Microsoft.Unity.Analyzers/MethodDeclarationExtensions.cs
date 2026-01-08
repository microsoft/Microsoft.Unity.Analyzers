/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Unity.Analyzers;

internal static class MethodDeclarationExtensions
{
	extension(MethodDeclarationSyntax method)
	{
		public bool HasPolymorphicModifier()
		{
			foreach (var modifier in method.Modifiers)
			{
				switch (modifier.Kind())
				{
					case SyntaxKind.AbstractKeyword:
					case SyntaxKind.VirtualKeyword:
					case SyntaxKind.OverrideKeyword:
						return true;
				}
			}

			return false;
		}
	}
}
