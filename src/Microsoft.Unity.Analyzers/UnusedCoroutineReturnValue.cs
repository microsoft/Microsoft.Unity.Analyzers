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

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnusedCoroutineReturnValueAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new(
			id: "UNT0012",
			title: Strings.UnusedCoroutineReturnValueDiagnosticTitle,
			messageFormat: Strings.UnusedCoroutineReturnValueDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.UnusedCoroutineReturnValueDiagnosticDescription);

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

			if (!IsValidCoroutine(symbol.Symbol, out var methodName))
				return;

			if (!invocation.Parent.IsKind(SyntaxKind.ExpressionStatement))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodName));
		}

		private static bool IsValidCoroutine(ISymbol symbol, out string methodName)
		{
			methodName = null;
			if (symbol is not IMethodSymbol method)
				return false;

			var containingType = method.ContainingType;
			if (!containingType.Extends(typeof(UnityEngine.MonoBehaviour)))
				return false;

			if (!method.ReturnType.Matches(typeof(System.Collections.IEnumerator)))
				return false;

			methodName = method.Name;
			return true;
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class UnusedCoroutineReturnValueCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UnusedCoroutineReturnValueAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var invocation = await context.GetFixableNodeAsync<InvocationExpressionSyntax>();
			if (invocation == null)
				return;

			var parent = invocation.Parent;
			if (!parent.IsKind(SyntaxKind.ExpressionStatement))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.UnusedCoroutineReturnValueCodeFixTitle,
					ct => WrapWithStartCoroutineAsync(context.Document, parent, invocation, ct),
					invocation.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> WrapWithStartCoroutineAsync(Document document,
			SyntaxNode parent,
			InvocationExpressionSyntax invocation,
			CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			var newExpressionStatement = ExpressionStatement(
				InvocationExpression(
					IdentifierName("StartCoroutine"),
					ArgumentList(
						SingletonSeparatedList(
							Argument(invocation)))));

			var newRoot = root.ReplaceNode(parent, newExpressionStatement);
			if (newRoot == null)
				return document;

			return document.WithSyntaxRoot(newRoot);
		}
	}
}
