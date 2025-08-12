/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CodeStyleSuppressor : DiagnosticSuppressor
{
	internal static readonly SuppressionDescriptor Rule = new(
		id: "USP0023",
		suppressedDiagnosticId: "IDE1006",
		justification: Strings.CodeStyleSuppressorJustification);

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (var diagnostic in context.ReportedDiagnostics)
		{
			AnalyzeDiagnostic(diagnostic, context);
		}
	}

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(Rule);

	private static void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
	{
		var syntaxTree = diagnostic.Location.SourceTree;
		if (syntaxTree == null)
			return;

		var methodDeclarationSyntax = context.GetSuppressibleNode<MethodDeclarationSyntax>(diagnostic);
		if (methodDeclarationSyntax == null)
			return;

		var model = context.GetSemanticModel(syntaxTree);
		if (model.GetDeclaredSymbol(methodDeclarationSyntax) is not IMethodSymbol methodSymbol)
			return;

		var typeSymbol = methodSymbol.ContainingType;
		if (!typeSymbol.Extends(typeof(UnityEngine.MonoBehaviour)))
			return;

		var scriptInfo = new ScriptInfo(methodSymbol.ContainingType);
		if (!scriptInfo.IsMessage(methodSymbol))
			return;

		context.ReportSuppression(Suppression.Create(Rule, diagnostic));
	}
}
