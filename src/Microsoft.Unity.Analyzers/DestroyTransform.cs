/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DestroyTransformAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0030";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.DestroyTransformDiagnosticTitle,
		messageFormat: Strings.DestroyTransformDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.DestroyTransformDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	internal static readonly HashSet<string> DestroyMethodNames = ["Destroy", "DestroyImmediate"];

	private static bool InvocationMatches(SyntaxNode node)
	{
		switch (node)
		{
			case InvocationExpressionSyntax ies:
				return InvocationMatches(ies.Expression);
			case MemberAccessExpressionSyntax maes:
				return InvocationMatches(maes.Name);
			case IdentifierNameSyntax ins:
				var text = ins.Identifier.Text;
				return DestroyMethodNames.Contains(text);
			default:
				return false;
		}
	}

	internal static bool InvocationMatches(InvocationExpressionSyntax invocation, [NotNullWhen(true)] out ExpressionSyntax? argument)
	{
		argument = null;

		if (!InvocationMatches(invocation))
			return false;

		argument = invocation
			.ArgumentList
			.Arguments
			.FirstOrDefault()?
			.Expression;

		return argument != null;
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not InvocationExpressionSyntax invocation)
			return;

		if (!InvocationMatches(invocation, out var argument))
			return;

		var model = context.SemanticModel;
		if (model.GetSymbolInfo(invocation.Expression).Symbol is not IMethodSymbol methodSymbol)
			return;

		var typeSymbol = methodSymbol.ContainingType;
		if (!typeSymbol.Matches(typeof(UnityEngine.Object)))
			return;

		var transformTypeSymbol = model
			.GetTypeInfo(argument)
			.Type;

		if (!transformTypeSymbol.Extends(typeof(UnityEngine.Transform)))
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class DestroyTransformCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DestroyTransformAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var invocation = await context.GetFixableNodeAsync<InvocationExpressionSyntax>();
		if (invocation == null)
			return;

		if (!DestroyTransformAnalyzer.InvocationMatches(invocation, out var argument))
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.DestroyTransformCodeFixTitle,
				ct => UseGameObjectAsync(context.Document, argument, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Document> UseGameObjectAsync(Document document, ExpressionSyntax argument, CancellationToken cancellationToken)
	{
		var gameObject = SyntaxFactory.IdentifierName("gameObject");
		var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, argument.WithoutTrailingTrivia(), gameObject);

		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
		editor.ReplaceNode(argument, memberAccess.WithTrailingTrivia(argument.GetTrailingTrivia()));

		return editor.GetChangedDocument();
	}
}
