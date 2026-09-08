/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TextMeshProSetTextAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0044";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.TextMeshProSetTextDiagnosticTitle,
		messageFormat: Strings.TextMeshProSetTextDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.TextMeshProSetTextDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterCompilationStartAction(startContext =>
		{
			var textType = startContext.Compilation.GetTypeByMetadataName(typeof(TMPro.TMP_Text).FullName!);
			if (textType == null)
				return;

			var overloads = textType.GetMembers("SetText")
				.OfType<IMethodSymbol>()
				.Where(method => !method.IsStatic && method.DeclaredAccessibility == Accessibility.Public)
				.ToImmutableArray();

			startContext.RegisterSyntaxNodeAction(c => AnalyzeAssignment(c, textType, overloads), SyntaxKind.SimpleAssignmentExpression);
			startContext.RegisterSyntaxNodeAction(c => AnalyzeInvocation(c, textType, overloads), SyntaxKind.InvocationExpression);
		});
	}

	private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context, INamedTypeSymbol textType, ImmutableArray<IMethodSymbol> overloads)
	{
		var syntax = (AssignmentExpressionSyntax)context.Node;
		var name = syntax.Left switch
		{
			MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
			IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
			_ => null
		};
		if (name != "text")
			return;

		if (context.SemanticModel.GetOperation(context.Node, context.CancellationToken) is not ISimpleAssignmentOperation
			{
				Target: IPropertyReferenceOperation target,
				Parent: IExpressionStatementOperation
			} assignment)
			return;

		if (target.Property.Name != "text" || !SymbolEqualityComparer.Default.Equals(target.Property.ContainingType, textType))
			return;

		AnalyzeText(context, assignment.Value, overloads);
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol textType, ImmutableArray<IMethodSymbol> overloads)
	{
		var syntax = (InvocationExpressionSyntax)context.Node;
		var name = syntax.Expression is MemberBindingExpressionSyntax binding ? binding.Name : syntax.GetMethodNameSyntax();
		if (name?.Identifier.ValueText != "SetText" || syntax.ArgumentList.Arguments.Count != 1)
			return;

		if (context.SemanticModel.GetOperation(context.Node, context.CancellationToken) is not IInvocationOperation invocation)
			return;

		var method = invocation.TargetMethod;
		if (method.Name != "SetText"
			|| !SymbolEqualityComparer.Default.Equals(method.ContainingType, textType)
			|| method.Parameters.Length == 0
			|| method.Parameters[0].Type.SpecialType != SpecialType.System_String)
			return;

		var source = invocation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 0);
		if (source != null)
			AnalyzeText(context, source.Value, overloads);
	}

	private static void AnalyzeText(SyntaxNodeAnalysisContext context, IOperation value, ImmutableArray<IMethodSymbol> overloads)
	{
		while (value is IConversionOperation conversion
			&& (conversion.Conversion.IsIdentity || conversion.IsImplicit && conversion.OperatorMethod == null))
			value = conversion.Operand;

		string? replacement = null;
		switch (value)
		{
			case IInvocationOperation invocation when invocation.TargetMethod.Name == nameof(ToString)
				&& invocation.Arguments.Length == 0
				&& invocation.Instance != null:
				if (invocation.TargetMethod.ContainingType.Matches(typeof(StringBuilder))
					&& HasBufferOverload(overloads, invocation.TargetMethod.ContainingType, 1))
				{
					replacement = "SetText(StringBuilder)";
				}
				else if (IsSupportedNumber(invocation.Instance) && HasNumericOverload(overloads, 1))
				{
					replacement = "SetText(string, float)";
				}
				break;

			case IObjectCreationOperation { Constructor: { } constructor } creation
				when constructor.ContainingType.SpecialType == SpecialType.System_String
				&& constructor.Parameters.Length is 1 or 3
				&& constructor.Parameters[0].Type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Char } bufferType:
				var buffer = creation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 0)?.Value;
				if (buffer != null
					&& !(buffer.ConstantValue.HasValue && buffer.ConstantValue.Value == null)
					&& HasBufferOverload(overloads, bufferType, constructor.Parameters.Length))
				{
					replacement = constructor.Parameters.Length == 1 ? "SetText(char[])" : "SetText(char[], int, int)";
				}
				break;

			case IInterpolatedStringOperation interpolation:
				var count = CountNumericInterpolations(interpolation);
				if (count > 0 && HasNumericOverload(overloads, count))
					replacement = count == 1 ? "SetText(string, float)" : "SetText(string, float, ...)";
				break;
		}

		if (replacement != null)
			context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), replacement));
	}

	private static bool HasBufferOverload(ImmutableArray<IMethodSymbol> overloads, ITypeSymbol bufferType, int parameterCount)
	{
		return overloads.Any(method => method.Parameters.Length == parameterCount
			&& SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, bufferType)
			&& method.Parameters.Skip(1).All(parameter => parameter.Type.SpecialType == SpecialType.System_Int32));
	}

	private static bool HasNumericOverload(ImmutableArray<IMethodSymbol> overloads, int valueCount)
	{
		return overloads.Any(method => method.Parameters.Length == valueCount + 1
			&& method.Parameters[0].Type.SpecialType == SpecialType.System_String
			&& method.Parameters.Skip(1).All(parameter => parameter.Type.SpecialType == SpecialType.System_Single));
	}

	private static int CountNumericInterpolations(IInterpolatedStringOperation interpolation)
	{
		var count = 0;
		foreach (var part in interpolation.Parts)
		{
			switch (part)
			{
				case IInterpolationOperation item:
					if (item.Alignment != null || item.FormatString != null || !IsSupportedNumber(item.Expression))
						return 0;

					count++;
					break;

				case IInterpolatedStringTextOperation { Text.ConstantValue: { HasValue: true, Value: string literal } }:
					if (literal.IndexOf('{') >= 0 || literal.IndexOf('}') >= 0)
						return 0;
					break;
			}
		}

		return count;
	}

	private static bool IsSupportedNumber(IOperation value)
	{
		if (value.Type?.SpecialType is not (SpecialType.System_Byte or SpecialType.System_SByte
			or SpecialType.System_Int16 or SpecialType.System_UInt16
			or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Single))
			return false;

		if (!value.ConstantValue.HasValue)
			return true;

		return value.ConstantValue.Value switch
		{
			int number => IsSupportedConstant(number),
			uint number => IsSupportedConstant(number),
			float number => IsSupportedConstant(number),
			_ => true
		};
	}

	private static bool IsSupportedConstant(double value)
	{
		var single = (float)value;
		// TMP converts through float, decimal, and long. These checks do not establish formatting equivalence.
		return value >= long.MinValue && value <= long.MaxValue
			&& single == value
			&& (float)(decimal)single == single;
	}
}
