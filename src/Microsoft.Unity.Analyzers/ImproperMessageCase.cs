/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
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
using Microsoft.CodeAnalysis.Rename;
using Microsoft.Unity.Analyzers.Resources;
using Document = Microsoft.CodeAnalysis.Document;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ImproperMessageCaseAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0033";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.ImproperMessageCaseDiagnosticTitle,
		messageFormat: Strings.ImproperMessageCaseDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.ImproperMessageCaseDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
	}

	private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not ClassDeclarationSyntax classSyntax)
			return;

		if (context.SemanticModel.GetDeclaredSymbol(classSyntax) is not ITypeSymbol typeSymbol)
			return;

		var scriptInfo = new ScriptInfo(typeSymbol);
		if (!scriptInfo.HasMessages)
			return;

		var methods = classSyntax.Members.OfType<MethodDeclarationSyntax>();

		var allMessages = scriptInfo
			.GetMessages()
			.ToLookup(m => m.Name); // case-sensitive indexing

		var notImplementedMessages = scriptInfo
			.GetNotImplementedMessages()
			.ToLookup(m => m.Name.ToLower()); // case-insensitive indexing

		foreach (var methodSyntax in methods)
		{
			if (methodSyntax.ExplicitInterfaceSpecifier != null)
				continue;

			if (methodSyntax.HasPolymorphicModifier())
				continue;

			var methodName = methodSyntax.Identifier.Text;
			// We have a valid case match here, so stop further inspection (This will prevent false positive for possible overloads, when one of them is still in the notImplementedMessages lookup)
			if (allMessages.Contains(methodName))
				continue;

			var key = methodName.ToLower();
			if (!notImplementedMessages.Contains(key))
				continue;

			if (context.SemanticModel.GetDeclaredSymbol(methodSyntax) is not IMethodSymbol methodSymbol)
				continue;

			var namedMessages = notImplementedMessages[key];
			if (namedMessages.All(m => m.IsStatic != methodSymbol.IsStatic))
				continue;

			// We can't use SymbolFinder.FindReferencesAsync() to find possible references, given we do not have access to the solution here yet
			context.ReportDiagnostic(Diagnostic.Create(Rule, methodSyntax.Identifier.GetLocation(), methodName));
		}
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class ImproperMessageCaseCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ImproperMessageCaseAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var declaration = await context.GetFixableNodeAsync<MethodDeclarationSyntax>();
		if (declaration == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.ImproperMessageCaseCodeFixTitle,
				ct => FixMessageCaseAsync(context.Document, declaration, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Solution> FixMessageCaseAsync(Document document, MethodDeclarationSyntax methodSyntax, CancellationToken cancellationToken)
	{
		var solution = document.Project.Solution;

		var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (model == null)
			return solution;

		if (model.GetDeclaredSymbol(methodSyntax) is not { } methodSymbol)
			return solution;

		var scriptInfo = new ScriptInfo(methodSymbol.ContainingType);
		var newName = scriptInfo
			.GetNotImplementedMessages()
			.FirstOrDefault(m => m.Name.Equals(methodSymbol.Name, StringComparison.OrdinalIgnoreCase))?.Name;

		if (newName == null)
			return solution;

		return await Renamer.RenameSymbolAsync(solution, methodSymbol, newName, solution.Options, cancellationToken);
	}
}
