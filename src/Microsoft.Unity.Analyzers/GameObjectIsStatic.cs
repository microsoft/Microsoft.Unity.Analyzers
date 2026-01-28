/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GameObjectIsStaticAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0040";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.GameObjectIsStaticDiagnosticTitle,
		messageFormat: Strings.GameObjectIsStaticDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.GameObjectIsStaticDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	private static readonly string[] EditorOnlyMessages = ["OnValidate", "Reset"];
	private static readonly Regex EditorFolderRegex = new(@"[/\\]Assets[/\\].*[/\\]Editor[/\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
	}

	private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not MemberAccessExpressionSyntax memberAccess)
			return;

		if (memberAccess.Name.Identifier.Text != "isStatic")
			return;

		var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
		if (typeInfo.Type == null)
			return;

		if (!typeInfo.Type.Matches(typeof(UnityEngine.GameObject)))
			return;

		if (DirectiveHelper.IsInsideDirective(memberAccess, "UNITY_EDITOR"))
			return;

		if (IsInsideEditorOnlyMessage(memberAccess, context.SemanticModel))
			return;

		if (IsInsideEditorFolder(context))
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.Name.GetLocation()));
	}

	private static bool IsInsideEditorFolder(SyntaxNodeAnalysisContext context)
	{
		var filePath = context.Node.SyntaxTree.FilePath;
		return !string.IsNullOrWhiteSpace(filePath) && EditorFolderRegex.IsMatch(filePath);
	}

	private static bool IsInsideEditorOnlyMessage(SyntaxNode node, SemanticModel semanticModel)
	{
		var methodSyntax = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
		if (methodSyntax == null)
			return false;

		var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax);
		if (methodSymbol == null)
			return false;

		var scriptInfo = new ScriptInfo(methodSymbol.ContainingType);
		if (!scriptInfo.HasMessages)
			return false;

		return EditorOnlyMessages.Contains(methodSymbol.Name) && scriptInfo.IsMessage(methodSymbol);
	}
}
