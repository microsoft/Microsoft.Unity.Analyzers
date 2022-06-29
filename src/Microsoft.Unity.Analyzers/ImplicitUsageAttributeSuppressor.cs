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
public class ImplicitUsageAttributeSuppressor : DiagnosticSuppressor
{
	internal static readonly SuppressionDescriptor Rule = new(
		id: "USP0019",
		suppressedDiagnosticId: "IDE0051",
		justification: Strings.ImplicitUsageAttributeSuppressorJustification);

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (var diagnostic in context.ReportedDiagnostics)
		{
			AnalyzeDiagnostic(diagnostic, context);
		}
	}

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(Rule);

	private void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
	{
		var methodDeclarationSyntax = context.GetSuppressibleNode<MethodDeclarationSyntax>(diagnostic);
		if (methodDeclarationSyntax == null)
			return;

		var syntaxTree = diagnostic.Location.SourceTree;
		if (syntaxTree == null)
			return;

		var model = context.GetSemanticModel(syntaxTree);
		if (model.GetDeclaredSymbol(methodDeclarationSyntax) is not IMethodSymbol methodSymbol)
			return;

		if (!IsSuppressable(methodSymbol))
			return;

		context.ReportSuppression(Suppression.Create(Rule, diagnostic));
	}

	private bool IsSuppressable(IMethodSymbol methodSymbol)
	{
		// The Unity code stripper will consider any attribute with the exact name "PreserveAttribute", regardless of the namespace or assembly
		return methodSymbol.GetAttributes().Any(a => a.AttributeClass != null && (a.AttributeClass.Matches(typeof(JetBrains.Annotations.UsedImplicitlyAttribute)) || a.AttributeClass.Name == nameof(UnityEngine.Scripting.PreserveAttribute)));
	}
}
