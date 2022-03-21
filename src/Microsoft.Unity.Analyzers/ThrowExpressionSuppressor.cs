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
public class ThrowExpressionSuppressor : DiagnosticSuppressor
{
	internal static readonly SuppressionDescriptor Rule = new(
		id: "USP0018",
		suppressedDiagnosticId: "IDE0016",
		justification: Strings.ThrowExpressionSuppressorJustification);

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
		var node = context.GetSuppressibleNode<ThrowStatementSyntax>(diagnostic);

		var ifStatement = node?
			.Ancestors()
			.OfType<IfStatementSyntax>()
			.FirstOrDefault();

		if (ifStatement?.Condition is not BinaryExpressionSyntax binaryExpression)
			return;

		var model = context.GetSemanticModel(diagnostic.Location.SourceTree);
		if (model == null)
			return;

		if (ShouldReportSuppression(binaryExpression.Left, model) || ShouldReportSuppression(binaryExpression.Right, model))
			context.ReportSuppression(Suppression.Create(Rule, diagnostic));
	}

	private static bool ShouldReportSuppression(ExpressionSyntax expression, SemanticModel model)
	{
		var type = model.GetTypeInfo(expression);
		return type.Type?.Extends(typeof(UnityEngine.Object)) ?? false;
	}
}
