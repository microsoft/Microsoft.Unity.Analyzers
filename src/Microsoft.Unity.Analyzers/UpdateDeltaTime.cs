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
	public class UpdateDeltaTimeAnalyzer : DiagnosticAnalyzer
	{
		public const string UpdateId = "UNT0004";

		public static readonly DiagnosticDescriptor UpdateRule = new DiagnosticDescriptor(
			UpdateId,
			title: Strings.UpdateWithoutFixedDeltaTimeDiagnosticTitle,
			messageFormat: Strings.UpdateWithoutFixedDeltaTimeDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.UpdateWithoutFixedDeltaTimeDiagnosticDescription);

		public const string FixedUpdateId = "UNT0005";

		public static readonly DiagnosticDescriptor FixedUpdateRule = new DiagnosticDescriptor(
			FixedUpdateId,
			title: Strings.FixedUpdateWithoutDeltaTimeDiagnosticTitle,
			messageFormat: Strings.FixedUpdateWithoutDeltaTimeDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.FixedUpdateWithoutDeltaTimeDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UpdateRule, FixedUpdateRule);

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

		private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
		{
			if (!(context.Node is MethodDeclarationSyntax method))
				return;

			if (IsMessage(context, method, typeof(UnityEngine.MonoBehaviour), "Update"))
				AnalyzeMemberAccess(context, method, "Time.fixedDeltaTime", UpdateRule);

			if (IsMessage(context, method, typeof(UnityEngine.MonoBehaviour), "FixedUpdate"))
				AnalyzeMemberAccess(context, method, "Time.deltaTime", FixedUpdateRule);
		}

		private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method, string memberName, DiagnosticDescriptor descriptor)
		{
			var usages = method
				.DescendantNodes()
				.OfType<MemberAccessExpressionSyntax>()
				.Where(expression => expression.ToString() == memberName);

			foreach (var usage in usages)
				context.ReportDiagnostic(Diagnostic.Create(descriptor, usage.Name.GetLocation()));
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class UpdateDeltaTimeCodeFix : CodeFixProvider
	{
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UpdateDeltaTimeAnalyzer.UpdateId, UpdateDeltaTimeAnalyzer.FixedUpdateId);

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root.FindNode(context.Span) is IdentifierNameSyntax identifierName))
				return;

			var diagnostic = context.Diagnostics.FirstOrDefault();
			if (diagnostic == null)
				return;

			switch (diagnostic.Id)
			{
				case UpdateDeltaTimeAnalyzer.UpdateId:
					context.RegisterCodeFix(
						CodeAction.Create(
							Strings.UpdateWithoutFixedDeltaTimeCodeFixTitle,
							ct => ReplaceDeltaTimeIdentifierAsync(context.Document, identifierName, "deltaTime", ct),
							identifierName.ToFullString()),
						context.Diagnostics);
					break;
				case UpdateDeltaTimeAnalyzer.FixedUpdateId:
					context.RegisterCodeFix(
						CodeAction.Create(
							Strings.FixedUpdateWithoutDeltaTimeCodeFixTitle,
							ct => ReplaceDeltaTimeIdentifierAsync(context.Document, identifierName, "fixedDeltaTime", ct),
							identifierName.ToFullString()),
						context.Diagnostics);
					break;
			}
		}

		protected async Task<Document> ReplaceDeltaTimeIdentifierAsync(Document document, IdentifierNameSyntax identifierName, string name, CancellationToken ct)
		{
			var root = await document
				.GetSyntaxRootAsync(ct)
				.ConfigureAwait(false);

			var newIdentifierName = identifierName.WithIdentifier(SyntaxFactory.Identifier(name));

			var newRoot = root.ReplaceNode(identifierName, newIdentifierName);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
