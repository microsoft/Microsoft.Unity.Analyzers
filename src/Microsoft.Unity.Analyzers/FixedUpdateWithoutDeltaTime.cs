using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class FixedUpdateWithoutDeltaTimeAnalyzer : BaseUpdateDeltaTimeAnalyzer
	{
		public const string Id = "UNT0005";

		protected sealed override DiagnosticDescriptor Rule => _rule;
		protected sealed override string MemberAccessSearch => "Time.deltaTime";
		protected override string UnityMessage => "FixedUpdate";

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(
			Id,
			title: Strings.FixedUpdateWithoutDeltaTimeDiagnosticTitle,
			messageFormat: Strings.FixedUpdateWithoutDeltaTimeDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.FixedUpdateWithoutDeltaTimeDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class FixedUpdateWithoutDeltaTimeCodeFix : BaseUpdateDeltaTimeCodeFix
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds =>
			ImmutableArray.Create(FixedUpdateWithoutDeltaTimeAnalyzer.Id);

		protected sealed override string NewDeltaTimeIdentifier => "fixedDeltaTime";

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root.FindNode(context.Span) is IdentifierNameSyntax identifierName))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.FixedUpdateWithoutDeltaTimeCodeFixTitle,
					ct => UseNewDeltaTimeIdentifier(context.Document, identifierName, ct),
					identifierName.ToFullString()),
				context.Diagnostics);
		}
	}
}
