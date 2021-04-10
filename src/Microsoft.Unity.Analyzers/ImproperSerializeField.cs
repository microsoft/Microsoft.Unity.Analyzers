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
		internal static readonly DiagnosticDescriptor Rule = new(
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
			var model = context.SemanticModel;
			ISymbol symbol;

			switch (context.Node)
			{
				case PropertyDeclarationSyntax pdec:
					symbol = model.GetDeclaredSymbol(pdec);
					break;
				case FieldDeclarationSyntax fdec:
					if (fdec.Declaration.Variables.Count == 0)
						return;

					// attributes are applied to all fields declaration symbols
					// just get the first one
					symbol = model.GetDeclaredSymbol(fdec.Declaration.Variables[0]);
					break;
				default:
					// we only support field/property analysis
					return;
			}

			if (!IsReportable(symbol))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), symbol.Name));
		}

		private static bool IsReportable(ISymbol symbol)
		{
			if (symbol == null)
				return false;

			if (!symbol.ContainingType.Extends(typeof(UnityEngine.Object)))
				return false;

			if (!symbol.GetAttributes().Any(a => a.AttributeClass.Matches(typeof(UnityEngine.SerializeField))))
				return false;

			return symbol switch
			{
				IFieldSymbol => symbol.DeclaredAccessibility == Accessibility.Public,// Only invalid on public fields
				IPropertySymbol => true,// Should never be on a property
				_ => false,
			};
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class ImproperSerializeFieldCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ImproperSerializeFieldAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var declaration = await context.GetFixableNodeAsync<MemberDeclarationSyntax>();
			if (declaration == null)
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
					var attributeType = model.GetTypeInfo(node).Type;
					if (attributeType == null)
						continue;

					if (attributeType.Matches(typeof(UnityEngine.SerializeField)))
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

			var newDeclaration = declaration
				.WithAttributeLists(attributes)
				.WithLeadingTrivia(declaration.GetLeadingTrivia());

			var newRoot = root.ReplaceNode(declaration, newDeclaration);
			if (newRoot == null)
				return document;

			return document.WithSyntaxRoot(newRoot);
		}
	}
}
