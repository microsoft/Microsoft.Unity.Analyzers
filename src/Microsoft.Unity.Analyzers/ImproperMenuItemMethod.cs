/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
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
public class ImproperMenuItemMethodAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0020";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.ImproperMenuItemMethodDiagnosticTitle,
		messageFormat: Strings.ImproperMenuItemMethodDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.ImproperMenuItemMethodDiagnosticDescription);

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

		if (context.SemanticModel.GetDeclaredSymbol(method) is not { } methodSymbol)
			return;

		if (!methodSymbol.GetAttributes().Any(a => a.AttributeClass != null && a.AttributeClass.Matches(typeof(UnityEditor.MenuItem))))
			return;

		if (methodSymbol.IsStatic)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, method.GetLocation(), method));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class ImproperMenuItemMethodCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => [ImproperMenuItemMethodAnalyzer.Rule.Id];

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var declaration = await context.GetFixableNodeAsync<MethodDeclarationSyntax>();
		if (declaration == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.ImproperMenuItemMethodCodeFixTitle,
				ct => AddStaticDeclarationAsync(context.Document, declaration, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Document> AddStaticDeclarationAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

		// we want to keep MenuCommands, so do not reset parameter list
		var newMethodDeclaration = methodDeclaration;

		if (!methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
		{
			newMethodDeclaration = newMethodDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
		}

		var newRoot = root?.ReplaceNode(methodDeclaration, newMethodDeclaration);
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}
}
