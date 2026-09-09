/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncVoidDelegateAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0046";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.AsyncVoidDelegateDiagnosticTitle,
		messageFormat: Strings.AsyncVoidDelegateDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.AsyncVoidDelegateDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterOperationAction(AnalyzeAnonymousFunction, OperationKind.AnonymousFunction);
	}

	private static void AnalyzeAnonymousFunction(OperationAnalysisContext context)
	{
		var function = (IAnonymousFunctionOperation)context.Operation;
		if (function.Symbol is not { IsAsync: true, ReturnsVoid: true })
			return;

		if (function.Syntax is AnonymousFunctionExpressionSyntax syntax)
			context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.AsyncKeyword.GetLocation()));
	}
}
