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
public class IndirectionMessageAnalyzer : DiagnosticAnalyzer
{
	internal static readonly DiagnosticDescriptor Rule = new(
		id: "UNT0019",
		title: Strings.IndirectionMessageDiagnosticTitle,
		messageFormat: Strings.IndirectionMessageDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: Strings.IndirectionMessageDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.SimpleMemberAccessExpression);
	}

	private static void AnalyzeExpression(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not MemberAccessExpressionSyntax access)
			return;

		if (access.Name.ToFullString() != "gameObject")
			return;

		var model = context.SemanticModel;
		var symbol = model.GetSymbolInfo(access);
		var typeInfo = model.GetTypeInfo(access.Expression);

		if (symbol.Symbol is not IPropertySymbol)
			return;

		if (typeInfo.Type == null)
			return;

		if (!typeInfo.Type.Extends(typeof(UnityEngine.GameObject)))
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), access.Name));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class IndirectionMessageCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IndirectionMessageAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var access = await context.GetFixableNodeAsync<MemberAccessExpressionSyntax>();
		if (access == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.IndirectionMessageCodeFixTitle,
				ct => DeleteIndirectionAsync(context.Document, access, ct),
				access.Expression.ToFullString()),
			context.Diagnostics);
	}

	private static async Task<Document> DeleteIndirectionAsync(Document document, MemberAccessExpressionSyntax access, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		var newExpression = access.Expression;
		var newRoot = root.ReplaceNode(access, newExpression);

		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}
}
