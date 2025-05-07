/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LoadAttributeMethodAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0015";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.LoadAttributeMethodDiagnosticTitle,
		messageFormat: Strings.LoadAttributeMethodDiagnosticMessageFormat,
		category: DiagnosticCategory.TypeSafety,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.LoadAttributeMethodDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
	}

	internal static bool MethodMatches(SyntaxNode? node, SemanticModel model, [NotNullWhen(true)] out MethodDeclarationSyntax? syntax, [NotNullWhen(true)] out IMethodSymbol? symbol)
	{
		syntax = null;
		symbol = null;

		if (node is not MethodDeclarationSyntax methodSyntax)
			return false;

		syntax = methodSyntax;

		if (model.GetDeclaredSymbol(methodSyntax) is not { } methodSymbol)
			return false;

		if (!IsDecorated(methodSymbol))
			return false;

		symbol = methodSymbol;
		return true;
	}

	internal static bool IsDecorated(IMethodSymbol symbol, bool onlyEditorAttributes = false)
	{
		return symbol
			.GetAttributes()
			.Any(a => a.AttributeClass != null && IsLoadAttributeType(a.AttributeClass, onlyEditorAttributes));
	}

	private static bool IsLoadAttributeType(ITypeSymbol type, bool onlyEditorAttributes)
	{
		return type.Matches(typeof(UnityEditor.InitializeOnLoadMethodAttribute))
			   || type.Matches(typeof(UnityEditor.Callbacks.DidReloadScripts))
			   || (type.Matches(typeof(UnityEngine.RuntimeInitializeOnLoadMethodAttribute)) && !onlyEditorAttributes);
	}


	private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
	{
		if (!MethodMatches(context.Node, context.SemanticModel, out var syntax, out var symbol))
			return;

		if (symbol is { IsStatic: true, Parameters.Length: 0 })
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.Identifier.GetLocation(), symbol.Name));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class LoadAttributeMethodCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => [LoadAttributeMethodAnalyzer.Rule.Id];

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var methodDeclaration = await context.GetFixableNodeAsync<MethodDeclarationSyntax>();
		if (methodDeclaration == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.LoadAttributeMethodCodeFixTitle,
				ct => FixMethodAsync(context.Document, methodDeclaration, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Document> FixMethodAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken ct)
	{
		var root = await document
			.GetSyntaxRootAsync(ct)
			.ConfigureAwait(false);

		var newMethodDeclaration = methodDeclaration
			.WithParameterList(methodDeclaration
				.ParameterList
				.WithParameters(SyntaxFactory.SeparatedList<ParameterSyntax>()));

		if (!methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
		{
			newMethodDeclaration = newMethodDeclaration
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
		}

		var newRoot = root?.ReplaceNode(methodDeclaration, newMethodDeclaration);
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}
}
