/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Collections.Immutable;
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
	public class MethodInvocationAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0015",
			title: Strings.MethodInvocationDiagnosticTitle,
			messageFormat: Strings.MethodInvocationDiagnosticMessageFormat,
			category: DiagnosticCategory.TypeSafety,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.MethodInvocationDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
		}

		// TODO we cannot add this to our stubs/KnownMethods so far (else they will be matched as Unity messages)
		private static readonly HashSet<string> MethodNames = new HashSet<string>(new[] {"Invoke", "InvokeRepeating", "StartCoroutine", "StopCoroutine"});

		private static bool InvocationMatches(SyntaxNode node)
		{
			switch (node)
			{
				case InvocationExpressionSyntax ies:
					return InvocationMatches(ies.Expression);
				case MemberAccessExpressionSyntax maes:
					return InvocationMatches(maes.Name);
				case IdentifierNameSyntax ins:
					return MethodNames.Contains(ins.Identifier.Text);
				default:
					return false;
			}
		}

		internal static bool InvocationMatches(InvocationExpressionSyntax ies, out string argument)
		{
			argument = null;

			if (!InvocationMatches(ies))
				return false;

			var args = ies.ArgumentList.Arguments;

			if (args.Count <= 0)
				return false;

			if (!(args.First().Expression is LiteralExpressionSyntax les))
				return false;

			argument = les.Token.ValueText;

			return true;
		}

		private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
		{
			if (!(context.Node is InvocationExpressionSyntax invocation))
				return;

			var options = invocation.SyntaxTree?.Options as CSharpParseOptions;
			if (options == null || options.LanguageVersion < LanguageVersion.CSharp6) // we want nameof support
				return;

			if (!InvocationMatches(invocation, out _))
				return;

			var model = context.SemanticModel;
			if (!(model.GetSymbolInfo(invocation.Expression).Symbol is IMethodSymbol methodSymbol))
				return;

			var typeSymbol = methodSymbol.ContainingType;
			if (!typeSymbol.Extends(typeof(UnityEngine.MonoBehaviour)))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class MethodInvocationCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MethodInvocationAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root.FindNode(context.Span) is InvocationExpressionSyntax invocation))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.MethodInvocationCodeFixTitle,
					ct => UseNameOfAsync(context.Document, invocation, ct),
					invocation.ToFullString()),
				context.Diagnostics);
		}

		private async Task<Document> UseNameOfAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			// We already know that we have at least one string argument
			var les = (LiteralExpressionSyntax)invocation.ArgumentList.Arguments[0].Expression;
			var name = les.Token.ValueText;

			var nameof = IdentifierName(
				Identifier(
					TriviaList(),
					SyntaxKind.NameOfKeyword,
					"nameof",
					"nameof",
					TriviaList()));

			var newArgument = Argument(
				InvocationExpression(nameof)
					.WithArgumentList(
						ArgumentList(
							SingletonSeparatedList(
								Argument(
									IdentifierName(name))))));

			var newInvocation = invocation
				.WithAdditionalAnnotations(Formatter.Annotation)
				.WithArgumentList(invocation.ArgumentList.ReplaceNode(invocation.ArgumentList.Arguments[0], newArgument));

			var newRoot = root.ReplaceNode(invocation, newInvocation);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
