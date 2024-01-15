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
public class PropertyDrawerOnGUIAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0027";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.PropertyDrawerOnGUIDiagnosticTitle,
		messageFormat: Strings.PropertyDrawerOnGUIDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.PropertyDrawerOnGUIDiagnosticDescription);

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
		if (name is not {Identifier.Text: "OnGUI"})
			return;

		var symbol = context.SemanticModel.GetSymbolInfo(invocation);

		if (symbol.Symbol is not IMethodSymbol method)
			return;

		if (method.ContainingType.ToDisplayString() != "UnityEditor.PropertyDrawer")
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), invocation));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class PropertyDrawerOnGUICodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PropertyDrawerOnGUIAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var invocation = await context.GetFixableNodeAsync<InvocationExpressionSyntax>();
		if (invocation == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.PropertyDrawerOnGUICodeFixTitle,
				ct => RemoveInvocationAsync(context.Document, invocation, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Document> RemoveInvocationAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

		var parent = invocation.Parent;
		if (parent == null)
			return document;

		var newRoot = root?.RemoveNode(parent, SyntaxRemoveOptions.KeepNoTrivia);
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}
}
