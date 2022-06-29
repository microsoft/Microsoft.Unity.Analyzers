/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
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
public class ProtectedUnityMessageAnalyzer : DiagnosticAnalyzer
{
	internal static readonly DiagnosticDescriptor Rule = new(
		id: "UNT0021",
		title: Strings.ProtectedUnityMessageDiagnosticTitle,
		messageFormat: Strings.ProtectedUnityMessageDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: false,
		description: Strings.ProtectedUnityMessageDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
	}

	private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
	{
		var method = context.Node as MethodDeclarationSyntax;
		if (method?.Body == null)
			return;

		if (method.Modifiers.Any(SyntaxKind.ProtectedKeyword))
			return;

		var classDeclaration = method.FirstAncestorOrSelf<ClassDeclarationSyntax>();
		if (classDeclaration == null || classDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword))
			return;

		var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
		if (typeSymbol == null)
			return;

		var scriptInfo = new ScriptInfo(typeSymbol);
		if (!scriptInfo.HasMessages)
			return;

		var symbol = context.SemanticModel.GetDeclaredSymbol(method);
		if (symbol == null)
			return;
		
		if (!scriptInfo.IsMessage(symbol))
			return;

		// In this case the scope is enforced
		if (symbol.IsOverride)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, method.Identifier.GetLocation()));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class ProtectedUnityMessageCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ProtectedUnityMessageAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var declaration = await context.GetFixableNodeAsync<MethodDeclarationSyntax>();
		if (declaration == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.ProtectedUnityMessageCodeFixTitle,
				ct => MakeMessageProtectedAsync(context.Document, declaration, ct),
				declaration.ToFullString()),
			context.Diagnostics);
	}

	private static async Task<Document> MakeMessageProtectedAsync(Document document, MethodDeclarationSyntax declaration, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		var newDeclaration = declaration
			.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)))
			.WithLeadingTrivia(declaration.GetLeadingTrivia());

		foreach (var modifier in declaration.Modifiers)
		{
			if (modifier.IsKind(SyntaxKind.PublicKeyword) || modifier.IsKind(SyntaxKind.PrivateKeyword) || modifier.IsKind(SyntaxKind.InternalKeyword))
				continue;

			var kind = modifier.Kind();
			newDeclaration = newDeclaration.AddModifiers(SyntaxFactory.Token(kind));
		}

		var newRoot = root?.ReplaceNode(declaration, newDeclaration);
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}
}
