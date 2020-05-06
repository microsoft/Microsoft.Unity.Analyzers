/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
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

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MethodInvocationAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0016",
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
		internal static readonly HashSet<string> InvokeMethodNames = new HashSet<string>(new[] {"Invoke", "InvokeRepeating"});
		internal static readonly HashSet<string> CoroutineMethodNames = new HashSet<string>(new[] {"StartCoroutine", "StopCoroutine"});

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
					return InvokeMethodNames.Contains(text) || CoroutineMethodNames.Contains(text);
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

		private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
		{
			if (!(context.Node is InvocationExpressionSyntax invocation))
				return;

			var options = invocation.SyntaxTree?.Options as CSharpParseOptions;
			if (options == null || options.LanguageVersion < LanguageVersion.CSharp6) // we want nameof support
				return;

			if (!InvocationMatches(invocation, out string argument))
				return;

			var model = context.SemanticModel;
			if (!(model.GetSymbolInfo(invocation.Expression).Symbol is IMethodSymbol methodSymbol))
				return;

			var typeSymbol = methodSymbol.ContainingType;
			if (!typeSymbol.Extends(typeof(UnityEngine.MonoBehaviour)))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), argument));
		}
	}

	public abstract class BaseMethodInvocationCodeFix : CodeFixProvider
	{
		public string Title { get; }

		protected BaseMethodInvocationCodeFix(string title)
		{
			Title = title;
		}

		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MethodInvocationAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root.FindNode(context.Span) is InvocationExpressionSyntax invocation))
				return;

			if (! await IsRegistrableAsync(context, invocation))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Title,
					ct => ChangeArgumentAsync(context.Document, invocation, ct),
					invocation.ToFullString()),
				context.Diagnostics);
		}

		protected virtual async Task<bool> IsRegistrableAsync(CodeFixContext context, InvocationExpressionSyntax invocation)
		{
			// for now we do not offer codefixes for mixed types
			var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(invocation.Expression is MemberAccessExpressionSyntax maes))
				return true;

			var typeInvocationContext = model.GetTypeInfo(maes.ChildNodes().FirstOrDefault()).Type as INamedTypeSymbol;
			if (typeInvocationContext == null)
				return false;

			var mdec = invocation
				.Ancestors()
				.OfType<MethodDeclarationSyntax>()
				.FirstOrDefault();

			if (mdec == null)
				return false;

			var symbol = model.GetDeclaredSymbol(mdec);
			var typeContext = symbol?.ContainingType;

			return typeContext != null && SymbolEqualityComparer.Default.Equals(typeContext, typeInvocationContext);
		}

		protected abstract ArgumentSyntax GetArgument(string name);

		private async Task<Document> ChangeArgumentAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			// We already know that we have at least one string argument
			var les = (LiteralExpressionSyntax)invocation.ArgumentList.Arguments[0].Expression;
			var name = les.Token.ValueText;

			var newInvocation = invocation
				.WithArgumentList(invocation.ArgumentList.ReplaceNode(invocation.ArgumentList.Arguments[0], GetArgument(name)));

			var newRoot = root.ReplaceNode(invocation, newInvocation);
			return document.WithSyntaxRoot(newRoot);
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class MethodInvocationNameOfCodeFix : BaseMethodInvocationCodeFix
	{
		public MethodInvocationNameOfCodeFix() : base(Strings.MethodInvocationNameOfCodeFixTitle)
		{
		}

		protected override ArgumentSyntax GetArgument(string name)
		{
			var nameof = IdentifierName(
				Identifier(
					TriviaList(),
					SyntaxKind.NameOfKeyword,
					"nameof",
					"nameof",
					TriviaList()));

			return Argument(
				InvocationExpression(nameof)
					.WithArgumentList(
						ArgumentList(
							SingletonSeparatedList(
								Argument(
									IdentifierName(name))))));
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class MethodInvocationDirectCallCodeFix : BaseMethodInvocationCodeFix
	{
		public MethodInvocationDirectCallCodeFix() : base(Strings.MethodInvocationDirectCallCodeFixTitle)
		{
		}

		protected override async Task<bool> IsRegistrableAsync(CodeFixContext context, InvocationExpressionSyntax invocation)
		{
			if (!await base.IsRegistrableAsync(context, invocation))
				return false;

			if (invocation.ArgumentList.Arguments.Count != 1)
				return false;

			var model = await context.Document.GetSemanticModelAsync();
			if (!(model.GetSymbolInfo(invocation.Expression).Symbol is IMethodSymbol methodSymbol))
				return false;

			return MethodInvocationAnalyzer.CoroutineMethodNames.Contains(methodSymbol.Name);
		}

		protected override ArgumentSyntax GetArgument(string name)
		{
			return Argument(InvocationExpression(IdentifierName(name)));
		}
	}
}
