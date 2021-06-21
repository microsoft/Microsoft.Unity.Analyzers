/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Unity.Analyzers
{
	internal static class InvocationExpressionSyntaxExtensions
	{
		public static SimpleNameSyntax? GetMethodNameSyntax(this InvocationExpressionSyntax expr)
		{
			SimpleNameSyntax? nameSyntax = null;

			switch (expr.Expression)
			{
				case MemberAccessExpressionSyntax mae:
					nameSyntax = mae.Name;
					break;
				case IdentifierNameSyntax ies:
					nameSyntax = ies;
					break;
			}

			return nameSyntax;
		}

	}
}
