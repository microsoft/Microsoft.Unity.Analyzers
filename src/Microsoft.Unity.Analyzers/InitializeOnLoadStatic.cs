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
	public class InitializeOnLoadStaticAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor StaticCtorRule = new DiagnosticDescriptor(
			id: "UNT0009",
			title: Strings.InitializeOnLoadStaticCtorDiagnosticTitle,
			messageFormat: Strings.InitializeOnLoadStaticCtorDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.InitializeOnLoadStaticCtorDiagnosticDescription);

		internal static readonly DiagnosticDescriptor StaticMethodRule = new DiagnosticDescriptor(
			id: "UNT0015",
			title: Strings.InitializeOnLoadStaticCtorDiagnosticTitle,
			messageFormat: Strings.InitializeOnLoadStaticCtorDiagnosticMessageFormat,
			category: DiagnosticCategory.Correctness,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.InitializeOnLoadStaticCtorDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(StaticCtorRule, StaticMethodRule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeDeclaration, SyntaxKind.ClassDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeDeclaration, SyntaxKind.MethodDeclaration);
		}

		private static void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
		{
			ISymbol symbol = null;

			switch (context.Node)
			{
				case ClassDeclarationSyntax classDeclaration:
					symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
					break;
				case MethodDeclarationSyntax methodDeclaration:
					symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
					break;
				case null:
					return;
			}

			var isInitOnLoad = symbol
				.GetAttributes()
				.Any(a => 
					 a.AttributeClass.Matches(typeof(UnityEditor.InitializeOnLoadAttribute)) ||
					 a.AttributeClass.Matches(typeof(UnityEditor.InitializeOnLoadMethodAttribute)) ||
					 a.AttributeClass.Matches(typeof(UnityEditor.RuntimeInitializeOnLoadStaticMethodAttribute)));

			if (!isInitOnLoad)
				return;

			switch (symbol)
			{
				case INamedTypeSymbol typeSymbol:
					if (!typeSymbol.StaticConstructors.Any(c => !c.IsImplicitlyDeclared))
						context.ReportDiagnostic(Diagnostic.Create(StaticCtorRule, ((ClassDeclarationSyntax)context.Node).Identifier.GetLocation(), typeSymbol.Name));
					break;
				case IMethodSymbol methodSymbol:
					if (!methodSymbol.IsStatic)
						context.ReportDiagnostic(Diagnostic.Create(StaticMethodRule, ((MethodDeclarationSyntax)context.Node).Identifier.GetLocation(), methodSymbol.Name));
					break;
			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class InitializeOnLoadStaticCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(InitializeOnLoadStaticAnalyzer.StaticCtorRule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root.FindNode(context.Span) is ClassDeclarationSyntax classDeclaration))
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
