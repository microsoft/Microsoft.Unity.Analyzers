/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Unity.Analyzers;

public abstract class BaseAttributeSuppressor : DiagnosticSuppressor
{

	protected abstract Type[] SuppressableAttributeTypes { get; }

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (var diagnostic in context.ReportedDiagnostics)
		{
			AnalyzeDiagnostic(diagnostic, context);
		}
	}

	private static bool IsSuppressableAttribute(INamedTypeSymbol? symbol, Type type)
	{
		return symbol != null && symbol.Matches(type);
	}

	protected virtual bool IsSuppressableAttribute(INamedTypeSymbol? symbol)
	{
		return SuppressableAttributeTypes.Any(type => IsSuppressableAttribute(symbol, type));
	}

	protected virtual bool IsSuppressable(ISymbol symbol)
	{
		return symbol.GetAttributes().Any(a => IsSuppressableAttribute(a.AttributeClass));
	}

	protected abstract SyntaxNode? GetSuppressibleNode(Diagnostic diagnostic, SuppressionAnalysisContext context);

	private void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
	{
		var node = GetSuppressibleNode(diagnostic, context);
		if (node == null)
			return;

		var syntaxTree = diagnostic.Location.SourceTree;
		if (syntaxTree == null)
			return;

		var model = context.GetSemanticModel(syntaxTree);
		var symbol = model.GetDeclaredSymbol(node);
		if (symbol == null)
			return;

		if (!IsSuppressable(symbol))
			return;

		foreach (var descriptor in SupportedSuppressions.Where(d => d.SuppressedDiagnosticId == diagnostic.Id))
			context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
	}
}
