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
    public class UpdateWithoutFixedDeltaTimeAnalyzer : BaseUpdateDeltaTimeAnalyzer
    {
        public const string Id = "UNT0004";

        protected sealed override DiagnosticDescriptor Rule => _rule;
        protected sealed override string MemberAccessSearch => "Time.fixedDeltaTime";
        protected override string UnityMessage => "Update";

        private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(
            Id,
            title: Strings.UpdateWithoutFixedDeltaTimeDiagnosticTitle,
            messageFormat: Strings.UpdateWithoutFixedDeltaTimeDiagnosticMessageFormat,
            category: DiagnosticCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Strings.UpdateWithoutFixedDeltaTimeDiagnosticDescription);
    }

    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class UpdateWithoutFixedDeltaTimeCodeFix : BaseUpdateDeltaTimeCodeFix
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UpdateWithoutFixedDeltaTimeAnalyzer.Id);
        protected sealed override string NewDeltaTimeIdentifier => "deltaTime";

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (!(root.FindNode(context.Span) is IdentifierNameSyntax identifierName))
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    Strings.UpdateWithoutFixedDeltaTimeCodeFixTitle,
                    ct => UseNewDeltaTimeIdentifier(context.Document, identifierName, ct),
                    identifierName.ToFullString()),
                context.Diagnostics);
        }
    }
}
