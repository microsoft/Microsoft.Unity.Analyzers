/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

//using System.Collections.Immutable;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CodeFixes;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Diagnostics;
//using Microsoft.Unity.Analyzers.Resources;
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
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class IndirectionMessageAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0019",
			title: Strings.IndirectionMessageDiagnosticTitle,
			messageFormat: Strings.IndirectionMessageDiagnosticMessageFormat,
			category: DiagnosticCategory.Performance,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.IndirectionMessageDiagnosticDescription);

		internal const string UpdateId = "UNT0019";

		private static readonly DiagnosticDescriptor GameObjectRule = new DiagnosticDescriptor(
			UpdateId,
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
			context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
		}


		private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
		{
			var invocation = (InvocationExpressionSyntax)context.Node;
			if (!(context.Node is MethodDeclarationSyntax method))
				return;

			var symbol = context.SemanticModel.GetSymbolInfo(invocation);
			if (symbol.Symbol == null)
				return;

			if (!(invocation.Expression is IdentifierNameSyntax))
				return;

			if (!invocation.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.TypeOfExpression))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodName));
		}


		[ExportCodeFixProvider(LanguageNames.CSharp)]
		public class IndirectionMessageCodeFix : CodeFixProvider
		{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IndirectionMessageAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root?.FindNode(context.Span) is InvocationExpressionSyntax invocation))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.IndirectionMessageCodeFixTitle,
					ct => UseGenericGetPropertyAsync(context.Document, invocation, ct),
					invocation.ToFullString()),
				context.Diagnostics);
			}

		private static async Task<Document> UseGenericGetPropertyAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
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
			var target = invocation;

			var newRoot = root.ReplaceNode(target, newInvocation.WithAdditionalAnnotations(Formatter.Annotation));
			if (newRoot == null)
				return document;

			return document.WithSyntaxRoot(newRoot);
		}
		}
	}
}
