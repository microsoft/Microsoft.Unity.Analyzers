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
public class GetComponentIncorrectTypeAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0014";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.GetComponentIncorrectTypeDiagnosticTitle,
		messageFormat: Strings.GetComponentIncorrectTypeDiagnosticMessageFormat,
		category: DiagnosticCategory.TypeSafety,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.GetComponentIncorrectTypeDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;
		var name = invocation.GetMethodNameSyntax();
		if (name == null)
			return;

		if (!KnownMethods.IsGetComponentName(name))
			return;

		var symbol = context.SemanticModel.GetSymbolInfo(invocation);

		if (symbol.Symbol is not IMethodSymbol method)
			return;

		if (!KnownMethods.IsGetComponent(method))
			return;

		if (!HasInvalidTypeArgument(method, out var identifier))
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), identifier));
	}

	private static bool HasInvalidTypeArgument(IMethodSymbol method, out string? identifier)
	{
		identifier = null;
		var argumentType = method.TypeArguments.FirstOrDefault();
		if (argumentType == null)
			return false;

		if (argumentType.Extends(typeof(UnityEngine.Component)) || argumentType.TypeKind == TypeKind.Interface)
			return false;

		if (argumentType.TypeKind == TypeKind.TypeParameter && argumentType is ITypeParameterSymbol typeParameter)
		{
			if (typeParameter.ConstraintTypes.Any(t => t.Extends(typeof(UnityEngine.Component))))
				return false;
		}

		identifier = argumentType.Name;
		return true;
	}
}
