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
public class UnusedMethodSuppressor : DiagnosticSuppressor
{
	internal static readonly SuppressionDescriptor Rule = new(
		id: "USP0008",
		suppressedDiagnosticId: "IDE0051",
		justification: Strings.UnusedMethodSuppressorJustification);

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

		while (typeSymbol.ContainingType != null && typeSymbol.ContainingType.Extends(typeof(UnityEngine.MonoBehaviour)))
			typeSymbol = typeSymbol.ContainingType;

		var report = typeSymbol.Locations
			.Select(location => location.SourceTree?.GetRoot(context.CancellationToken).FindNode(location.SourceSpan))
			.SelectMany(typeNode => typeNode?.DescendantNodes())
			.OfType<InvocationExpressionSyntax>()
			.Any(e => MethodInvocationAnalyzer.InvocationMatches(e, out string? argument) && argument == methodSymbol.Name);

		if (report)
			context.ReportSuppression(Suppression.Create(Rule, diagnostic));
	}
}
