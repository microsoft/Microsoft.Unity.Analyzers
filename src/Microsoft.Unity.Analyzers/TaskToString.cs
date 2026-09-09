/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TaskToStringAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0047";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.TaskToStringDiagnosticTitle,
		messageFormat: Strings.TaskToStringDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.TaskToStringDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterOperationAction(AnalyzeInterpolation, OperationKind.Interpolation);
		context.RegisterOperationAction(AnalyzeConcatenation, OperationKind.BinaryOperator, OperationKind.CompoundAssignment);
		context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
	}

	private static void AnalyzeInterpolation(OperationAnalysisContext context)
	{
		ReportTask(context, ((IInterpolationOperation)context.Operation).Expression);
	}

	private static void AnalyzeConcatenation(OperationAnalysisContext context)
	{
		if (context.Operation.Type?.SpecialType != SpecialType.System_String)
			return;

		switch (context.Operation)
		{
			case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add, OperatorMethod: null } binary:
				ReportTask(context, binary.LeftOperand);
				ReportTask(context, binary.RightOperand);
				break;

			case ICompoundAssignmentOperation { OperatorKind: BinaryOperatorKind.Add, OperatorMethod: null } assignment:
				ReportTask(context, assignment.Value);
				break;
		}
	}

	private static void AnalyzeInvocation(OperationAnalysisContext context)
	{
		var invocation = (IInvocationOperation)context.Operation;
		var method = invocation.TargetMethod;

		if (method is { Name: nameof(ToString), Arity: 0, MethodKind: MethodKind.Ordinary, ReturnType.SpecialType: SpecialType.System_String }
			&& method.Parameters.IsEmpty && invocation.Instance != null)
		{
			ReportTask(context, invocation.Instance);
			return;
		}

		if (!FormatsArguments(method))
			return;

		foreach (var argument in invocation.Arguments)
		{
			if (argument.Parameter?.Type.SpecialType == SpecialType.System_Object)
			{
				ReportTask(context, argument.Value);
			}
			else if (argument.Parameter?.Type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Object }
				&& UnwrapImplicitConversions(argument.Value) is IArrayCreationOperation { Initializer: { } initializer })
			{
				foreach (var value in initializer.ElementValues)
					ReportTask(context, value);
			}
		}
	}

	private static bool FormatsArguments(IMethodSymbol method)
	{
		var type = method.ContainingType;
		return type.SpecialType == SpecialType.System_String && method.Name is nameof(string.Format) or nameof(string.Concat)
			|| type.Matches(typeof(StringBuilder)) && method.Name is nameof(StringBuilder.Append) or nameof(StringBuilder.AppendFormat)
			|| type.Name == "Console" && type.ContainingNamespace.ToDisplayString() == "System" && method.Name is "Write" or "WriteLine"
			|| type.Matches(typeof(UnityEngine.Debug)) && method.Name is "Log" or "LogWarning" or "LogError" or "LogFormat" or "LogWarningFormat" or "LogErrorFormat";
	}

	private static IOperation UnwrapImplicitConversions(IOperation value)
	{
		while (value is IConversionOperation { IsImplicit: true, OperatorMethod: null } conversion)
			value = conversion.Operand;

		return value;
	}

	private static void ReportTask(OperationAnalysisContext context, IOperation value)
	{
		value = UnwrapImplicitConversions(value);
		if (value.Type?.IsTaskLike() == true)
			context.ReportDiagnostic(Diagnostic.Create(Rule, value.Syntax.GetLocation()));
	}
}
