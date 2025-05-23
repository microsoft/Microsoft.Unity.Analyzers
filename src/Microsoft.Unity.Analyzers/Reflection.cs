/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReflectionAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0018";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.ReflectionDiagnosticTitle,
		messageFormat: Strings.ReflectionDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.ReflectionDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
	}

	private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not MethodDeclarationSyntax method)
			return;

		if (!IsCriticalMessage(context, method))
			return;

		AnalyzeMethodBody(context, method);
	}

	private static bool IsCriticalMessage(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method)
	{
		var methodSymbol = context.SemanticModel.GetDeclaredSymbol(method);
		if (methodSymbol == null)
			return false;

		var classDeclaration = method.FirstAncestorOrSelf<ClassDeclarationSyntax>();
		if (classDeclaration == null)
			return false;

		var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
		if (typeSymbol == null)
			return false;

		var scriptInfo = new ScriptInfo(typeSymbol);
		if (!scriptInfo.HasMessages)
			return false;

		if (!scriptInfo.IsMessage(methodSymbol))
			return false;

		return methodSymbol.Name switch
		{
			"Update" or "FixedUpdate" or "LateUpdate" or "OnGUI" => true,
			_ => false,
		};
	}

	private static void AnalyzeMethodBody(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method)
	{
		var srns = typeof(Assembly).Namespace;

		var body = method.Body;
		if (body?.Statements == null)
			return;

		if (body.Statements.Count <= 0)
			return;

		foreach (var node in body.DescendantNodesAndSelf().OfType<ExpressionSyntax>())
		{
			var typeInfo = context.SemanticModel.GetTypeInfo(node);
			var typeSymbol = typeInfo.Type;

			if (typeSymbol?.ContainingNamespace == null)
				continue;

			if (typeSymbol.ContainingNamespace.ToDisplayString() != srns)
				continue;

			context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), method.Identifier.Text));
			return;
		}
	}
}
