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
	public class InitializeOnLoadStaticCtorAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0009",
			title: Strings.InitializeOnLoadStaticCtorDiagnosticTitle,
			messageFormat: Strings.InitializeOnLoadStaticCtorDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.InitializeOnLoadStaticCtorDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
		}

		private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
		{
			if (!(context.Node is ClassDeclarationSyntax classDeclaration))
				return;

			var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

			var isInitOnLoad = typeSymbol
				.GetAttributes()
				.Any(a => a.AttributeClass.Matches(typeof(UnityEditor.InitializeOnLoadAttribute)));

			if (!isInitOnLoad)
				return;

			// Beware of compiler-generated ctor with static field initializers
			if (typeSymbol.StaticConstructors.Any(c => !c.IsImplicitlyDeclared))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), typeSymbol.Name));
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class InitializeOnLoadStaticCtorCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(InitializeOnLoadStaticCtorAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root?.FindNode(context.Span) is ClassDeclarationSyntax classDeclaration))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.InitializeOnLoadStaticCtorCodeFixTitle,
					ct => CreateStaticCtorAsync(context.Document, classDeclaration, ct),
					classDeclaration.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> CreateStaticCtorAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken ct)
		{
			var root = await document
				.GetSyntaxRootAsync(ct)
				.ConfigureAwait(false);

			var emptyStaticConstructor = SyntaxFactory.ConstructorDeclaration(classDeclaration.Identifier)
				.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
				.WithBody(SyntaxFactory.Block());

			var newClassDeclaration = classDeclaration
				.WithMembers(classDeclaration.Members.Insert(0, emptyStaticConstructor));

			var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
