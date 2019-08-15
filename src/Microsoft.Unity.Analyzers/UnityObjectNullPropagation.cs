using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnityObjectNullPropagationAnalyzer : DiagnosticAnalyzer
	{
		public const string Id = "UNT0008";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			Id,
			title: Strings.UnityObjectNullPropagationDiagnosticTitle,
			messageFormat: Strings.UnityObjectNullPropagationDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.UnityObjectNullPropagationDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeConditionalAccessExpression, SyntaxKind.ConditionalAccessExpression);
		}

		private void AnalyzeConditionalAccessExpression(SyntaxNodeAnalysisContext context)
		{
			var access = (ConditionalAccessExpressionSyntax)context.Node;
			var type = context.SemanticModel.GetTypeInfo(access.Expression);
			if (type.Type == null)
				return;

			if (!UnityObjectNullCoalescingAnalyzer.IsUnityObject(type.Type))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, access.GetLocation(), access.ToFullString()));
		}
	}

	//[ExportCodeFixProvider(LanguageNames.CSharp)]
	//public class UnityObjectNullPropagationCodeFix : CodeFixProvider
	//{
	//    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UnityObjectNullPropagationAnalyzer.Id);

	//    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	//    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	//    {
	//        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

	//        var declaration = root.FindNode(context.Span) as MethodDeclarationSyntax;
	//        if (declaration == null)
	//            return;

	//        context.RegisterCodeFix(
	//            CodeAction.Create(
	//                Strings.UnityObjectNullPropagationCodeFixTitle,
	//                ct => {},
	//                declaration.ToFullString()),
	//            context.Diagnostics);
	//    }
	//}
}
