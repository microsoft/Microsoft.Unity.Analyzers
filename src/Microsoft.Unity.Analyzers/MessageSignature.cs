/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MessageSignatureAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0006";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.MessageSignatureDiagnosticTitle,
		messageFormat: Strings.MessageSignatureDiagnosticMessageFormat,
		category: DiagnosticCategory.TypeSafety,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.MessageSignatureDiagnosticDescription);

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

		if (context.SemanticModel.GetDeclaredSymbol(classSyntax) is not { } typeSymbol)
			return;

		var scriptInfo = new ScriptInfo(typeSymbol);
		if (!scriptInfo.HasMessages)
			return;

		var methods = classSyntax.Members.OfType<MethodDeclarationSyntax>();
		var messages = scriptInfo
			.GetMessages()
			.ToLookup(m => m.Name);

		foreach (var methodSyntax in methods)
		{
			// Exclude explicit interface implementations, that will not be called by the Unity runtime
			// scriptInfo.IsMessage is already handling this through fullname, but for this very specific task we are looking for bad signatures, so not relying on scriptInfo.IsMessage at this step
			if (methodSyntax.ExplicitInterfaceSpecifier != null)
				continue;

			var methodName = methodSyntax.Identifier.Text;

			if (!messages.Contains(methodName))
				continue;

			if (context.SemanticModel.GetDeclaredSymbol(methodSyntax) is not { } methodSymbol)
				continue;

			// A message is detected, so the signature is correct
			if (scriptInfo.IsMessage(methodSymbol))
				continue;

			// Check static/instance compatibility
			var namedMessages = messages[methodName];
			if (namedMessages.All(m => m.IsStatic != methodSymbol.IsStatic))
				continue;

			context.ReportDiagnostic(Diagnostic.Create(Rule, methodSyntax.Identifier.GetLocation(), methodName));
		}
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class MessageSignatureCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MessageSignatureAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var method = await context.GetFixableNodeAsync<MethodDeclarationSyntax>();
		if (method == null)
			return;

		// Do not provide a code fix if the message is -wrongly- referenced elsewhere
		if (await context.IsReferencedAsync(method))
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.MessageSignatureCodeFixTitle,
				ct => FixMethodDeclarationSignatureAsync(context.Document, method, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Document> FixMethodDeclarationSignatureAsync(Document document, MethodDeclarationSyntax methodSyntax, CancellationToken ct)
	{
		var root = await document
			.GetSyntaxRootAsync(ct)
			.ConfigureAwait(false);

		var semanticModel = await document
			.GetSemanticModelAsync(ct)
			.ConfigureAwait(false);

		if (semanticModel.GetDeclaredSymbol(methodSyntax) is not { } methodSymbol)
			return document;

		var scriptInfo = new ScriptInfo(methodSymbol.ContainingType);
		var message = scriptInfo
			.GetMessages()
			.FirstOrDefault(m => m.Name == methodSymbol.Name);

		if (message == null)
			return document;

		if (document.Project.LanguageServices.GetService<SyntaxGenerator>() is not { } syntaxGenerator)
			return document;

		var builder = new MessageBuilder(syntaxGenerator);
		var newParameterList = CreateParameterList(builder, message)
			.WithCloseParenToken(
				SyntaxFactory.Token(SyntaxKind.CloseParenToken)
					.WithTrailingTrivia(methodSyntax.ParameterList.CloseParenToken.TrailingTrivia));
		var newMethodSyntax = methodSyntax
			.WithParameterList(newParameterList);

		var newRoot = root?.ReplaceNode(methodSyntax, newMethodSyntax);
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}

	private static ParameterListSyntax CreateParameterList(MessageBuilder builder, MethodInfo message)
	{
		return SyntaxFactory
			.ParameterList()
			.AddParameters([.. builder.CreateParameters(message).OfType<ParameterSyntax>()]);
	}
}
