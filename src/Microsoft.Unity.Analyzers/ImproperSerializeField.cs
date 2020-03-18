using System.Collections.Generic;
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
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0013",
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
			context.RegisterSyntaxNodeAction(AnalyzeMemberDeclaration, SyntaxKind.PropertyDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeMemberDeclaration, SyntaxKind.FieldDeclaration);
		}

		private static void AnalyzeMemberDeclaration(SyntaxNodeAnalysisContext context)
		{
			var symbols = new List<ISymbol>();
			var model = context.SemanticModel;

			switch (context.Node)
			{
				case PropertyDeclarationSyntax pdec:
					symbols.Add(model.GetDeclaredSymbol(pdec));
					break;
				case FieldDeclarationSyntax fdec:
					symbols.AddRange(fdec.Declaration.Variables.Select(v => model.GetDeclaredSymbol(v)));
					break;
				default:
					// we only support field/property analysis
					return;
			}

			var reportableSymbols = symbols
				.Where(IsReportable)
				.ToList();

			if (!reportableSymbols.Any())
				return;

			var name = string.Join(", ", reportableSymbols.Select(s => s.Name));
			context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), name));
		}

		private static bool IsReportable(ISymbol symbol)
		{
			if (symbol == null)
				return false;

			var containingType = symbol.ContainingType;
			if (!containingType.Extends(typeof(UnityEngine.Object)))
				return false;

			if (!symbol
				.GetAttributes()
				.Any(a => a.AttributeClass.Matches(typeof(UnityEngine.SerializeField))))
				return false;

			switch (symbol)
			{
				case IFieldSymbol _:
					return symbol.DeclaredAccessibility == Accessibility.Public;
				case IPropertySymbol _:
					return true;
				default:
					return false;
			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class ImproperSerializeFieldCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ImproperSerializeFieldAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root.FindNode(context.Span) is MemberDeclarationSyntax declaration))
				return;

			if (!(declaration is PropertyDeclarationSyntax || declaration is FieldDeclarationSyntax))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.ImproperSerializeFieldCodeFixTitle,
					ct => RemoveSerializeFieldAttributeAsync(context.Document, declaration, ct),
					declaration.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> RemoveSerializeFieldAttributeAsync(Document document, MemberDeclarationSyntax declaration, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

			var attributes = new SyntaxList<AttributeListSyntax>();

			foreach (var attributeList in declaration.AttributeLists)
			{
				var nodes = new List<AttributeSyntax>();
				foreach (var node in attributeList.Attributes)
				{
					var attributeType = model.GetTypeInfo(node);
					if (attributeType.Type.Matches(typeof(UnityEngine.SerializeField)))
						nodes.Add(node);
				}

				if (nodes.Any())
				{
					var newAttributes = attributeList.RemoveNodes(nodes, SyntaxRemoveOptions.KeepNoTrivia);
					if (newAttributes.Attributes.Any())
						attributes = attributes.Add(newAttributes);
				}
				else
					attributes = attributes.Add(attributeList);
			}

			var newDeclaration = declaration.WithAttributeLists(attributes);
			var newRoot = root.ReplaceNode(declaration, newDeclaration);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
