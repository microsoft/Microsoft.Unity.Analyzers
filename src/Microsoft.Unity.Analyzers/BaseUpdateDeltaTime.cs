using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Unity.Analyzers
{
	public abstract class BaseUpdateDeltaTimeAnalyzer : DiagnosticAnalyzer
	{
		protected abstract DiagnosticDescriptor Rule { get; }

		protected abstract string MemberAccessSearch { get; }
		protected abstract string UnityMessage { get; }

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
		}

		private static bool IsMessage(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method, Type metadata, string methodName)
		{
			var classDeclaration = method?.FirstAncestorOrSelf<ClassDeclarationSyntax>();
			if (classDeclaration == null)
				return false;

			var methodSymbol = context.SemanticModel.GetDeclaredSymbol(method);

			var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
			var scriptInfo = new ScriptInfo(typeSymbol);
			if (!scriptInfo.HasMessages)
				return false;

			if (!scriptInfo.IsMessage(methodSymbol))
				return false;

			return scriptInfo.Metadata == metadata && methodSymbol.Name == methodName;
		}

		private bool IsMonoBehaviourUpdateMessage(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method)
		{
			return IsMessage(context, method, typeof(UnityEngine.MonoBehaviour), UnityMessage);
		}

		private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
		{
			if (!(context.Node is MethodDeclarationSyntax method))
				return;

			if (!IsMonoBehaviourUpdateMessage(context, method))
				return;

			var usages = method
				.DescendantNodes()
				.OfType<MemberAccessExpressionSyntax>()
				.Where(expression => expression.ToString() == MemberAccessSearch);

			foreach (var usage in usages)
				context.ReportDiagnostic(Diagnostic.Create(Rule, usage.Name.GetLocation()));
		}
	}

	public abstract class BaseUpdateDeltaTimeCodeFix : CodeFixProvider
	{
		protected abstract string NewDeltaTimeIdentifier { get; }

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		protected async Task<Document> UseNewDeltaTimeIdentifier(Document document, IdentifierNameSyntax identifierName, CancellationToken ct)
		{
			var root = await document
				.GetSyntaxRootAsync(ct)
				.ConfigureAwait(false);

			var newIdentifierName = identifierName.WithIdentifier(SyntaxFactory.Identifier(NewDeltaTimeIdentifier));

			var newRoot = root.ReplaceNode(identifierName, newIdentifierName);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
