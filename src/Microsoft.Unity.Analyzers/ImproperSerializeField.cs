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
	public class ImproperSerializeFieldAnalyzer : DiagnosticAnalyzer
	{
		public const string Id = "UNT0013";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			Id,
			title: Strings.ImproperSerializeFieldDiagnosticTitle,
			messageFormat: Strings.ImproperSerializeFieldDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.ImproperSerializeFieldDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeDeclaration, SyntaxKind.PropertyDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeDeclaration, SyntaxKind.FieldDeclaration);
		}

		private static void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
		{
			var member = (MemberDeclarationSyntax)context.Node;
			if (!(member is PropertyDeclarationSyntax || member is FieldDeclarationSyntax))
				return;

			var symbol = context.SemanticModel.GetSymbolInfo(member);
			if (symbol.Symbol != null)
				return;

			if (!HasInvalidSerializeFieldAttribute(symbol.Symbol, out var memberName))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, member.GetLocation(), memberName));
		}

		private static bool HasInvalidSerializeFieldAttribute(ISymbol symbol, out string memberName)
		{
			memberName = null;
			if (!(symbol is IPropertySymbol || (symbol is IFieldSymbol && symbol.DeclaredAccessibility == Accessibility.Public)))
				return false;

			var containingType = symbol.ContainingType;
			if (!containingType.Extends(typeof(UnityEngine.Object)))
				return false;

			var hasSerializeFieldAttribute = symbol
				.GetAttributes()
				.Any(a => a.AttributeClass.Matches(typeof(UnityEngine.SerializeField)));

			if (!hasSerializeFieldAttribute)
				return false;

			memberName = symbol.Name;
			return true;
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class ImproperSerializeFieldCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ImproperSerializeFieldAnalyzer.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			var declaration = root.FindNode(context.Span) as MemberDeclarationSyntax;
			if (declaration == null)
				return;

			if (!(declaration is PropertyDeclarationSyntax || declaration is FieldDeclarationSyntax))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.ImproperSerializeFieldCodeFixTitle,
					ct => RemoveSerializeFieldAttribute(context.Document, declaration, ct),
					declaration.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> RemoveSerializeFieldAttribute(Document document, MemberDeclarationSyntax declaration, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			var attributes = new SyntaxList<AttributeListSyntax>();

			foreach (var attributeList in declaration.AttributeLists)
			{
				var nodes = attributeList
					.Attributes
					.Where(a => a.Name.ToString() == "SerializeField");

				if (nodes.Count() > 0)
				{
					var newAttributes = attributeList.RemoveNodes(nodes, SyntaxRemoveOptions.KeepNoTrivia);
					attributes.Add(newAttributes);
				}
			}

			var newDeclaration = declaration.WithAttributeLists(attributes);
			var newRoot = root.ReplaceNode(declaration, newDeclaration);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
