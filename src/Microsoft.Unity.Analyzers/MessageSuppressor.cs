/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MessageSuppressor : DiagnosticSuppressor
{
	internal static readonly SuppressionDescriptor MethodRule = new(
		id: "USP0003",
		suppressedDiagnosticId: "IDE0051",
		justification: Strings.MessageSuppressorJustification);

	internal static readonly SuppressionDescriptor MethodCrefRule = new(
		id: "USP0020",
		suppressedDiagnosticId: "IDE0052",
		justification: Strings.MessageSuppressorJustification);

	internal static readonly SuppressionDescriptor MethodCodeQualityRule = new(
		id: "USP0014",
		suppressedDiagnosticId: "CA1822",
		justification: Strings.MessageSuppressorJustification);

	internal static readonly SuppressionDescriptor ParameterRule = new(
		id: "USP0005",
		suppressedDiagnosticId: "IDE0060",
		justification: Strings.MessageSuppressorJustification);

	// This CA1801 rule has been deprecated in favor of IDE0060, keep it for legacy compatibility
	internal static readonly SuppressionDescriptor ParameterCodeQualityRule = new(
		id: "USP0015",
		suppressedDiagnosticId: "CA1801",
		justification: Strings.MessageSuppressorJustification);

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(
		MethodRule,
		MethodCrefRule,
		MethodCodeQualityRule,
		ParameterRule,
		ParameterCodeQualityRule
	);

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (var diagnostic in context.ReportedDiagnostics)
		{
			AnalyzeDiagnostic(diagnostic, context);
		}
	}

	private void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
	{
		var node = context.GetSuppressibleNode<SyntaxNode>(diagnostic, n => n is ParameterSyntax or MethodDeclarationSyntax);

		if (node is ParameterSyntax)
		{
			node = node
				.Ancestors()
				.OfType<MethodDeclarationSyntax>()
				.FirstOrDefault();
		}

		if (node == null)
			return;

		if (diagnostic.Location.SourceTree is not { } syntaxTree)
			return;

		var model = context.GetSemanticModel(syntaxTree);
		if (model.GetDeclaredSymbol(node) is not IMethodSymbol methodSymbol)
			return;

		var scriptInfo = new ScriptInfo(methodSymbol.ContainingType);
		if (!scriptInfo.IsMessage(methodSymbol))
			return;

		foreach (var suppression in SupportedSuppressions)
		{
			if (suppression.SuppressedDiagnosticId == diagnostic.Id)
				context.ReportSuppression(Suppression.Create(suppression, diagnostic));
		}
	}
}
