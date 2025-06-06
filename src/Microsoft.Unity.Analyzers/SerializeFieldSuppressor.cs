﻿/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;
using Unity.Properties;
using UnityEngine;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SerializeFieldSuppressor : DiagnosticSuppressor
{
	internal static readonly SuppressionDescriptor ReadonlyRule = new(
		id: "USP0004",
		suppressedDiagnosticId: "IDE0044",
		justification: Strings.ReadonlySerializeFieldSuppressorJustification);

	internal static readonly SuppressionDescriptor UnusedRule = new(
		id: "USP0006",
		suppressedDiagnosticId: "IDE0051",
		justification: Strings.UnusedSerializeFieldSuppressorJustification);

	internal static readonly SuppressionDescriptor UnusedCodeQualityRule = new(
		id: "USP0013",
		suppressedDiagnosticId: "CA1823",
		justification: Strings.UnusedSerializeFieldSuppressorJustification);

	internal static readonly SuppressionDescriptor NeverAssignedRule = new(
		id: "USP0007",
		suppressedDiagnosticId: "CS0649",
		justification: Strings.NeverAssignedSerializeFieldSuppressorJustification);

	private static readonly Type[] _suppressableAttributeTypes = [typeof(SerializeField), typeof(SerializeReference), typeof(CreatePropertyAttribute)];

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [ReadonlyRule, UnusedRule, UnusedCodeQualityRule, NeverAssignedRule];

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

	private static bool IsSuppressableAttribute(INamedTypeSymbol? symbol)
	{
		return _suppressableAttributeTypes.Any(type => IsSuppressableAttribute(symbol, type));
	}

	private static bool IsSuppressable(IFieldSymbol fieldSymbol)
	{
		if (fieldSymbol.GetAttributes().Any(a => IsSuppressableAttribute(a.AttributeClass)))
			return true;

		if (fieldSymbol.DeclaredAccessibility == Accessibility.Public && fieldSymbol.ContainingType.Extends(typeof(UnityEngine.Object)))
			return true;

		return false;
	}

	private void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
	{
		var fieldDeclarationSyntax = context.GetSuppressibleNode<VariableDeclaratorSyntax>(diagnostic);
		if (fieldDeclarationSyntax == null)
			return;

		var syntaxTree = diagnostic.Location.SourceTree;
		if (syntaxTree == null)
			return;

		var model = context.GetSemanticModel(syntaxTree);
		if (model.GetDeclaredSymbol(fieldDeclarationSyntax) is not IFieldSymbol fieldSymbol)
			return;

		if (!IsSuppressable(fieldSymbol))
			return;

		foreach (var descriptor in SupportedSuppressions.Where(d => d.SuppressedDiagnosticId == diagnostic.Id))
			context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
	}
}
