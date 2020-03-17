/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Unity.Analyzers
{
	internal static class MethodDeclarationSyntaxExtensions
	{
		public static bool IsMessage(this MethodDeclarationSyntax method, SyntaxNodeAnalysisContext context, Type metadata, string methodName)
		{
			var classDeclaration = method?.FirstAncestorOrSelf<ClassDeclarationSyntax>();
			if (classDeclaration == null)
				return false;

			var methodSymbol = context.SemanticModel.GetDeclaredSymbol(method);

			var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
			var scriptInfo = new ScriptInfo(typeSymbol);
			if (!scriptInfo.HasMessages)
				return false;

			if (!scriptInfo.IsMessage(methodSymbol))
				return false;

			return scriptInfo.Metadata == metadata && methodSymbol.Name == methodName;
		}
	}
}
