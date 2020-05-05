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
	public class InitializeOnLoadMethodAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0015",
			title: Strings.InitializeOnLoadMethodDiagnosticTitle,
			messageFormat: Strings.InitializeOnLoadMethodDiagnosticMessageFormat,
			category: DiagnosticCategory.TypeSafety,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.InitializeOnLoadMethodDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
		}

		internal static bool MethodMatches(SyntaxNode node, SemanticModel model, out MethodDeclarationSyntax syntax, out IMethodSymbol symbol)
		{
			syntax = null;
			symbol = null;

			if (!(node is MethodDeclarationSyntax methodSyntax))
				return false;

			syntax = methodSyntax;

			if (!(model.GetDeclaredSymbol(methodSyntax) is IMethodSymbol methodSymbol))
				return false;

			if (!IsDecorated(methodSymbol))
				return false;

			symbol = methodSymbol;
			return true;
		}

		private static bool IsDecorated(ISymbol symbol)
		{
			return symbol
				.GetAttributes()
				.Any(a => IsInitializeOnLoadMethodAttributeType(a.AttributeClass));
		}

		private static bool IsInitializeOnLoadMethodAttributeType(ITypeSymbol type)
		{
			return type.Matches(typeof(UnityEditor.InitializeOnLoadMethodAttribute))
			       || type.Matches(typeof(UnityEngine.RuntimeInitializeOnLoadMethodAttribute));
		}


		private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
		{
			if (!MethodMatches(context.Node, context.SemanticModel, out var syntax, out var symbol))
				return;

			if (symbol.IsStatic && symbol.Parameters.Length == 0)
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.Identifier.GetLocation(), symbol.Name));
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class InitializeOnLoadMethodCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(InitializeOnLoadMethodAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root.FindNode(context.Span) is MethodDeclarationSyntax methodDeclaration))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.InitializeOnLoadMethodCodeFixTitle,
					ct => FixMethodAsync(context.Document, methodDeclaration, ct),
					methodDeclaration.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> FixMethodAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken ct)
		{
			var root = await document
				.GetSyntaxRootAsync(ct)
				.ConfigureAwait(false);

			var newMethodDeclaration = methodDeclaration
				.WithParameterList(SyntaxFactory.ParameterList());

			if (!methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
			{
				newMethodDeclaration = newMethodDeclaration
					.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
			}

			var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
