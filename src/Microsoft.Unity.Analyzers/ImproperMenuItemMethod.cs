/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

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
	public class ImproperMenuItemMethodAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0019",
			title: Strings.ImproperMenuItemMethodDiagnosticTitle,
			messageFormat: Strings.ImproperMenuItemMethodDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.ImproperMenuItemMethodDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
		}
		private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
		{
			// check if it is of method declaration syntax
			// check if the attribute thing is a Menu Item
			// check if the method specifies static
			// later check: if method references this anywhere --> what is code fix then?

			if (!(context.Node is MethodDeclarationSyntax))
				return;
			
			var method = (MethodDeclarationSyntax)context.Node;
			var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(method);

			if (!(declaredSymbol is IMethodSymbol methodSymbol))
				return;

			if (!declaredSymbol.GetAttributes().Any(a => a.AttributeClass.Matches(typeof(UnityEditor.MenuItem))))
				return;

			if (declaredSymbol.IsStatic)
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, method.GetLocation(), method));
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class ImproperMenuItemMethodCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ImproperMenuItemMethodAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			var declaration = root.FindNode(context.Span) as MethodDeclarationSyntax;
			if (declaration == null)
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.ImproperMenuItemMethodCodeFixTitle,
					ct => AddStaticDeclarationAsync(context.Document, declaration, ct),
					declaration.ToFullString()),
				context.Diagnostics);
		}
		private static async Task<Document> AddStaticDeclarationAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			var newMethodDeclaration = methodDeclaration.WithParameterList(SyntaxFactory.ParameterList());

			if (!methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				newMethodDeclaration = newMethodDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
			}

			var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
			if (newRoot == null)
				return document;

			return document.WithSyntaxRoot(newRoot);
		}
	}
}
