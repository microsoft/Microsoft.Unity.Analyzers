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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.Unity.Analyzers.Resources;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NonGenericGetComponentAnalyzer : BaseGetComponentAnalyzer
{
	private const string RuleId = "UNT0003";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.NonGenericGetComponentDiagnosticTitle,
		messageFormat: Strings.NonGenericGetComponentDiagnosticMessageFormat,
		category: DiagnosticCategory.TypeSafety,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.NonGenericGetComponentDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

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
		if (name == null)
			return;

		if (!KnownMethods.IsGetComponentName(name))
			return;

		if (!IsNonGenericGetComponent(invocation, context.SemanticModel, out var method))
			return;

		if (invocation.Expression is not IdentifierNameSyntax)
			return;

		if (!invocation.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.TypeOfExpression))
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), method.Name));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class NonGenericGetComponentCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => [NonGenericGetComponentAnalyzer.Rule.Id];

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var invocation = await context.GetFixableNodeAsync<InvocationExpressionSyntax>();
		if (invocation == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.NonGenericGetComponentCodeFixTitle,
				ct => UseGenericGetComponentAsync(context.Document, invocation, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Document> UseGenericGetComponentAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

		var invocationArgumentList = invocation.ArgumentList;
		var syntaxList = invocationArgumentList.Arguments;

		var argumentSyntax = syntaxList.FirstOrDefault();
		if (argumentSyntax == null)
			return document;

		var typeOf = (TypeOfExpressionSyntax)argumentSyntax.Expression;
		var identifierSyntax = (IdentifierNameSyntax)invocation.Expression;

		var newArgumentList = invocationArgumentList.RemoveNode(argumentSyntax, SyntaxRemoveOptions.KeepNoTrivia);
		if (newArgumentList == null)
			return document;

		var newInvocation = invocation
			.WithExpression(GenericName(
				identifierSyntax.Identifier,
				TypeArgumentList(
					SeparatedList([typeOf.Type]))))
			.WithArgumentList(newArgumentList);

		// If we're casting the GetComponent result, remove the cast as the returned value is now type safe
		var target = IsParentCastingResult(invocation)
			? invocation.Parent
			: invocation;

		if (target == null)
			return document;

		var newRoot = root?.ReplaceNode(target, newInvocation.WithAdditionalAnnotations(Formatter.Annotation));
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}

	private static bool IsParentCastingResult(InvocationExpressionSyntax invocation)
	{
		return invocation.Parent switch
		{
			CastExpressionSyntax => true,
			BinaryExpressionSyntax be => be.IsKind(SyntaxKind.AsExpression),
			_ => false,
		};
	}
}
