/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Unity.Analyzers;

internal static class MethodDeclarationExtensions
{
	public static bool HasPolymorphicModifier(this MethodDeclarationSyntax method)
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
