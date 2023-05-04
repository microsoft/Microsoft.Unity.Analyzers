/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AssetOperationInLoadAttributeMethodAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0031";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.AssetOperationInLoadAttributeMethodDiagnosticTitle,
		messageFormat: Strings.AssetOperationInLoadAttributeMethodDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.AssetOperationInLoadAttributeMethodDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		
		context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
	}

	private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not MemberAccessExpressionSyntax {Expression: IdentifierNameSyntax ins})
			return;

		var text = ins.Identifier.Text;
		if (text != nameof(UnityEditor.AssetDatabase))
			return;

		var typeInfo = context
			.SemanticModel
			.GetTypeInfo(ins);

		if (typeInfo.Type == null || !typeInfo.Type.Extends(typeof(UnityEditor.AssetDatabase)))
			return;

		if (context.ContainingSymbol is not IMethodSymbol methodSymbol)
			return;

		var typeSymbol = methodSymbol.ContainingType;

		if (IsDecoratedMethod(methodSymbol) || IsStaticCtorInDecoratedType(methodSymbol, typeSymbol))
			context.ReportDiagnostic(Diagnostic.Create(Rule, ins.GetLocation()));
	}

	private static bool IsStaticCtorInDecoratedType(IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol) => typeSymbol.StaticConstructors.Contains(methodSymbol)
	                                                                                                            && InitializeOnLoadStaticCtorAnalyzer.IsDecorated(typeSymbol);

	private static bool IsDecoratedMethod(IMethodSymbol methodSymbol) => LoadAttributeMethodAnalyzer.IsDecorated(methodSymbol, onlyEditorAttributes: true);
}
