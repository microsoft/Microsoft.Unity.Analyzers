/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NonGenericGetComponentAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new(
			id: "UNT0003",
			title: Strings.NonGenericGetComponentDiagnosticTitle,
			messageFormat: Strings.NonGenericGetComponentDiagnosticMessageFormat,
			category: DiagnosticCategory.TypeSafety,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.NonGenericGetComponentDiagnosticDescription);

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
			var symbol = context.SemanticModel.GetSymbolInfo(invocation);
			if (symbol.Symbol == null)
				return;

			if (!IsNonGenericGetComponent(symbol.Symbol, out var methodName))
				return;

			if (invocation.Expression is not IdentifierNameSyntax)
				return;

			if (!invocation.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.TypeOfExpression))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodName));
		}

		private static bool IsNonGenericGetComponent(ISymbol symbol, [NotNullWhen(true)] out string? methodName)
		{
			methodName = null;
			if (symbol is not IMethodSymbol method)
				return false;

			if (!KnownMethods.IsGetComponent(method))
				return false;

			if (method.Parameters.Length == 0 || !method.Parameters[0].Type.Matches(typeof(Type)))
				return false;

			methodName = method.Name;
			return true;
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class NonGenericGetComponentCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NonGenericGetComponentAnalyzer.Rule.Id);

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
					invocation.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> UseGenericGetComponentAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			var typeOf = (TypeOfExpressionSyntax)invocation.ArgumentList.Arguments[0].Expression;
			var identifierSyntax = (IdentifierNameSyntax)invocation.Expression;

			var newInvocation = invocation
				.WithExpression(GenericName(
					identifierSyntax.Identifier,
					TypeArgumentList(
						SeparatedList(new[] { typeOf.Type }))))
				.WithArgumentList(invocation.ArgumentList.Arguments.Count == 0
					? ArgumentList()
					: invocation.ArgumentList.RemoveNode(invocation.ArgumentList.Arguments[0], SyntaxRemoveOptions.KeepNoTrivia));

			// If we're casting the GetComponent result, remove the cast as the returned value is now type safe
			var target = IsParentCastingResult(invocation)
				? invocation.Parent
				: invocation;

			var newRoot = root.ReplaceNode(target, newInvocation.WithAdditionalAnnotations(Formatter.Annotation));
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
}
