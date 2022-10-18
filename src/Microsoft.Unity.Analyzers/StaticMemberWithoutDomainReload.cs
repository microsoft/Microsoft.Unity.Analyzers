/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StaticMemberWithoutDomainReloadAnalyzer : DiagnosticAnalyzer
{
	public const string PreprocessorSymbolDisableDomainReload = "VSTU_DISABLE_DOMAIN_RELOAD";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: "UNT0033",
		title: Strings.StaticMemberWithoutDomainReloadDiagnosticTitle,
		messageFormat: Strings.StaticMemberWithoutDomainReloadDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: Strings.StaticMemberWithoutDomainReloadDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.AddAssignmentExpression);
		context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.SubtractAssignmentExpression);
		context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.SimpleAssignmentExpression);
		context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.PostIncrementExpression);
		context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.PostDecrementExpression);
		context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.PreIncrementExpression);
		context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.PreDecrementExpression);
	}
	
	private static void AnalyzeExpression(SyntaxNodeAnalysisContext context)
	{
		var node = context.Node;
		if (!node.SyntaxTree.Options.PreprocessorSymbolNames.Contains(PreprocessorSymbolDisableDomainReload))
			return;

		var expression = node switch
		{
			PostfixUnaryExpressionSyntax postfixUnaryExpressionSyntax => postfixUnaryExpressionSyntax.Operand,
			PrefixUnaryExpressionSyntax prefixUnaryExpressionSyntax => prefixUnaryExpressionSyntax.Operand,
			AssignmentExpressionSyntax assignmentExpression => assignmentExpression.Left,
			_ => null
		};

		if (expression == null)
			return;

		var model = context.SemanticModel;
		var symbol = model.GetSymbolInfo(expression);

		var candidate = symbol.Symbol;
		if (candidate is IFieldSymbol or IEventSymbol && candidate.IsStatic)
			context.ReportDiagnostic(Diagnostic.Create(Rule, expression.GetLocation(), candidate.Name));
	}
}
