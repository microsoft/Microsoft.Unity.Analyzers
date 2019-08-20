using System;
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
	public class NonGenericGetComponentAnalyzer : DiagnosticAnalyzer
	{
		public const string Id = "UNT0003";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			Id,
			title: Strings.NonGenericGetComponentDiagnosticTitle,
			messageFormat: Strings.NonGenericGetComponentDiagnosticMessageFormat,
			category: DiagnosticCategory.TypeSafety,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.NonGenericGetComponentDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		private static readonly HashSet<string> MethodNames = new HashSet<string>(new[]
		{
			"GetComponent",
			"GetComponents",
			"GetComponentInChildren",
			"GetComponentsInChildren",
			"GetComponentInParent",
			"GetComponentsInParent",
		});

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
		}

		private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
		{
			var invocation = (InvocationExpressionSyntax)context.Node;
			var symbol = context.SemanticModel.GetSymbolInfo(invocation);
			if (symbol.Symbol == null)
				return;

			string methodName;
			if (!IsNonGenericGetComponent(symbol.Symbol, out methodName))
				return;

			if (!(invocation.Expression is IdentifierNameSyntax))
				return;

			if (!invocation.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.TypeOfExpression))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodName));
		}

		private static bool IsNonGenericGetComponent(ISymbol symbol, out string methodName)
		{
			methodName = null;
			var method = symbol as IMethodSymbol;
			if (method == null)
				return false;

			var containingType = method.ContainingType;
			if (!containingType.Matches(typeof(UnityEngine.Component)) && !containingType.Matches(typeof(UnityEngine.GameObject)))
				return false;

			if (!MethodNames.Contains(method.Name))
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
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NonGenericGetComponentAnalyzer.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			var invocation = root.FindNode(context.Span) as InvocationExpressionSyntax;
			if (invocation == null)
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.NonGenericGetComponentCodeFixTitle,
					ct => UseGenericGetComponent(context.Document, invocation, ct),
					invocation.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> UseGenericGetComponent(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			var typeOf = (TypeOfExpressionSyntax)invocation.ArgumentList.Arguments[0].Expression;
			var identifierSyntax = (IdentifierNameSyntax)invocation.Expression;

			var newInvocation = invocation
				.WithExpression(GenericName(
					identifierSyntax.Identifier,
					TypeArgumentList(
						SeparatedList(new[] {typeOf.Type}))))
				.WithArgumentList(invocation.ArgumentList.Arguments.Count == 0
					? ArgumentList()
					: invocation.ArgumentList.RemoveNode(invocation.ArgumentList.Arguments[0], SyntaxRemoveOptions.KeepNoTrivia));

			// If we're casting the GetComponent result, remove the cast as the returned value is now type safe
			var target = IsParentCastingResult(invocation)
				? invocation.Parent
				: invocation;

			var newRoot = root.ReplaceNode(target, newInvocation.WithAdditionalAnnotations(Formatter.Annotation));
			return document.WithSyntaxRoot(newRoot);
		}

		private static bool IsParentCastingResult(InvocationExpressionSyntax invocation)
		{
			switch (invocation.Parent)
			{
				case CastExpressionSyntax _: return true;
				case BinaryExpressionSyntax be: return be.IsKind(SyntaxKind.AsExpression);
				default: return false;
			}
		}
	}
}
