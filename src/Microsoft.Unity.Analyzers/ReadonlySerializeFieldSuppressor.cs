using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ReadonlySerializeFieldSuppressor : DiagnosticSuppressor
	{
		private static readonly SuppressionDescriptor Rule = new SuppressionDescriptor(
			id: "USP0004",
			suppressedDiagnosticId: "IDE0044",
			justification: Strings.ReadonlySerializeFieldSuppressorJustification);

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
			if (node == null)
				return;

			var model = context.GetSemanticModel(diagnostic.Location.SourceTree);
			var fieldSymbol = model.GetDeclaredSymbol(node) as IFieldSymbol;
			if (fieldSymbol == null)
				return;

			if (fieldSymbol.GetAttributes().Any(a => a.AttributeClass.Matches(typeof(UnityEngine.SerializeField))))
				context.ReportSuppression(Suppression.Create(Rule, diagnostic));
		}
	}
}
