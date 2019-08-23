using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class UnusedMessageSuppressor : DiagnosticSuppressor
	{
		private static readonly SuppressionDescriptor Rule = new SuppressionDescriptor(
			id: "USP0003",
			suppressedDiagnosticId: "IDE0051",
			justification: Strings.UnusedMessageSuppressorJustification);

		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(Rule);

		public override void ReportSuppressions(SuppressionAnalysisContext context)
		{
			foreach (var diagnostic in context.ReportedDiagnostics)
			{
				AnalyzeDiagnostic(diagnostic, context);
			}
		}

		private void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
		{
			var node = diagnostic.Location.SourceTree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);
			var method = node as MethodDeclarationSyntax;
			if (method == null)
				return;

			var model = context.GetSemanticModel(diagnostic.Location.SourceTree);
			var methodSymbol = model.GetDeclaredSymbol(method) as IMethodSymbol;
			if (methodSymbol == null)
				return;

			var scriptInfo = new ScriptInfo(methodSymbol.ContainingType);
			if (scriptInfo.IsMessage(methodSymbol))
				context.ReportSuppression(Suppression.Create(Rule, diagnostic));
		}
	}
}
