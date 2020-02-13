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
			context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
		}

		private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
		{
			var property = (MemberDeclarationSyntax)context.Node;
			if (!(property is PropertyDeclarationSyntax))
				return;

			var symbol = context.SemanticModel.GetDeclaredSymbol(property);
			if (symbol == null)
				return;

			if (!PropertyHasInvalidSerializeField(symbol, out var propertyName))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, property.GetLocation(), propertyName));
		}

		private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
		{
			var field = (FieldDeclarationSyntax)context.Node;
			if (!(field is FieldDeclarationSyntax))
				return;

			List<ISymbol> symbols = new List<ISymbol>();
			foreach (var variable in field.Declaration.Variables)
			{
				var symbol = context.SemanticModel.GetDeclaredSymbol(variable);
				if (symbol != null)
					symbols.Add(symbol);
			}

			if (symbols.FirstOrDefault() == null)
				return;

			if (!FieldHasInvalidSerializeField(symbols, out var fields))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, field.GetLocation(), fields));
		}

		private static bool PropertyHasInvalidSerializeField(ISymbol symbol, out string propertyName)
		{
			propertyName = null;
			if (!(symbol is IPropertySymbol))
				return false;

			var containingType = symbol.ContainingType;
			if (!containingType.Extends(typeof(UnityEngine.Object)))
				return false;

			bool hasSerializeFieldAttribute = symbol
				.GetAttributes()
				.Any(a => a.AttributeClass.Matches(typeof(UnityEngine.SerializeField)));

			if (!hasSerializeFieldAttribute)
				return false;

			propertyName = symbol.Name;
			return true;
		}

		private static bool FieldHasInvalidSerializeField(List<ISymbol> symbols, out string fields)
		{
			fields = null;
			List<string> fieldNames = new List<string>();

			bool foundMatchingSymbol = false;
			foreach (var symbol in symbols)
			{
				fieldNames.Add(symbol.Name);

				if (!(symbol is IFieldSymbol && symbol.DeclaredAccessibility == Accessibility.Public))
					continue;

				var containingType = symbol.ContainingType;
				if (!containingType.Extends(typeof(UnityEngine.Object)))
					continue;

				if (!symbol.GetAttributes().Any(a => a.AttributeClass.Matches(typeof(UnityEngine.SerializeField))))
					continue;

				foundMatchingSymbol = true;
			}

			fields = string.Join(", ", fieldNames.ToArray());
			return foundMatchingSymbol;
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
					ct => RemoveSerializeFieldAttribute(context.Document, context, declaration, ct),
					declaration.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> RemoveSerializeFieldAttribute(Document document, CodeFixContext context, MemberDeclarationSyntax declaration, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

			var attributes = new SyntaxList<AttributeListSyntax>();

			foreach (var attributeList in declaration.AttributeLists)
			{
				List<AttributeSyntax> nodes = new List<AttributeSyntax>();
				foreach (var node in attributeList.Attributes)
				{
					var attributeType = model.GetTypeInfo(node);
					if (attributeType.Type.Matches(typeof(UnityEngine.SerializeField)))
						nodes.Add(node);
				}

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
