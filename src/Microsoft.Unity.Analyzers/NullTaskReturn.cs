/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullTaskReturnAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0048";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.NullTaskReturnDiagnosticTitle,
		messageFormat: Strings.NullTaskReturnDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.NullTaskReturnDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterOperationAction(AnalyzeReturn, OperationKind.Return);
	}

	private static void AnalyzeReturn(OperationAnalysisContext context)
	{
		var operation = (IReturnOperation)context.Operation;
		if (operation.ReturnedValue is not { Type: { IsReferenceType: true } type } value || !type.IsTaskLike())
			return;

		// An async return supplies the result, not the task. Use the nearest function,
		// since a lambda or local function can have a different async contract.
		if (GetContainingMethod(operation, context.ContainingSymbol)?.IsAsync == true)
			return;

		if (CanReturnNull(value))
			context.ReportDiagnostic(Diagnostic.Create(Rule, value.Syntax.GetLocation()));
	}

	private static IMethodSymbol? GetContainingMethod(IOperation operation, ISymbol containingSymbol)
	{
		for (var parent = operation.Parent; parent != null; parent = parent.Parent)
		{
			switch (parent)
			{
				case IAnonymousFunctionOperation anonymous:
					return anonymous.Symbol;
				case ILocalFunctionOperation local:
					return local.Symbol;
			}
		}

		return containingSymbol as IMethodSymbol;
	}

	private static bool CanReturnNull(IOperation value)
	{
		if (value.ConstantValue is { HasValue: true, Value: null })
			return true;

		return value switch
		{
			IConversionOperation { OperatorMethod: null } conversion => CanReturnNull(conversion.Operand),
			IParenthesizedOperation parentheses => CanReturnNull(parentheses.Operand),
			IDefaultValueOperation { Type.IsReferenceType: true } => true,
			IConditionalOperation { WhenFalse: { } whenFalse, Condition.ConstantValue: { HasValue: true, Value: bool condition } } conditional =>
				CanReturnNull(condition ? conditional.WhenTrue : whenFalse),
			IConditionalOperation { WhenFalse: { } whenFalse } conditional =>
				CanReturnNull(conditional.WhenTrue) || CanReturnNull(whenFalse),
			ISwitchExpressionOperation expression => expression.Arms.Any(arm => CanReturnNull(arm.Value)),
			ICoalesceOperation coalesce => CanReturnNull(coalesce.WhenNull),
			IConditionalAccessOperation => true,
			_ => false
		};
	}
}
