/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MessageSignatureAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0006",
			title: Strings.MessageSignatureDiagnosticTitle,
			messageFormat: Strings.MessageSignatureDiagnosticMessageFormat,
			category: DiagnosticCategory.TypeSafety,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.MessageSignatureDiagnosticDescription);

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
			var scriptInfo = new ScriptInfo(typeSymbol);
			if (!scriptInfo.HasMessages)
				return;

			var methods = classDeclaration.Members.OfType<MethodDeclarationSyntax>();
			var messages = scriptInfo
				.GetMessages()
				.ToLookup(m => m.Name);

			foreach (var method in methods)
			{
				var methodName = method.Identifier.Text;

				if (!messages.Contains(methodName))
					continue;

				var methodSymbol = context.SemanticModel.GetDeclaredSymbol(method);
				if (scriptInfo.IsMessage(methodSymbol))
					continue;

				context.ReportDiagnostic(Diagnostic.Create(Rule, method.Identifier.GetLocation(), methodName));
			}
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class MessageSignatureCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MessageSignatureAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root?.FindNode(context.Span) is MethodDeclarationSyntax methodDeclaration))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.MessageSignatureCodeFixTitle,
					ct => FixMethodDeclarationSignatureAsync(context.Document, methodDeclaration, ct),
					methodDeclaration.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> FixMethodDeclarationSignatureAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken ct)
		{
			var root = await document
				.GetSyntaxRootAsync(ct)
				.ConfigureAwait(false);

			var semanticModel = await document
				.GetSemanticModelAsync(ct)
				.ConfigureAwait(false);

			var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
			var typeSymbol = methodSymbol.ContainingType;

			var scriptInfo = new ScriptInfo(typeSymbol);
			var message = scriptInfo
				.GetMessages()
				.First(m => m.Name == methodSymbol.Name);

			var builder = new MessageBuilder(document.Project.LanguageServices.GetService<SyntaxGenerator>());
			var newMethodDeclaration = methodDeclaration
				.WithParameterList(CreateParameterList(builder, message));

			var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
			return document.WithSyntaxRoot(newRoot);
		}

		private static ParameterListSyntax CreateParameterList(MessageBuilder builder, MethodInfo message)
		{
			return SyntaxFactory
				.ParameterList()
				.AddParameters(builder.CreateParameters(message).OfType<ParameterSyntax>().ToArray());
		}
	}
}
