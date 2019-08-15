using System;
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
using UnityEngine;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CreateInstanceAnalyzer : DiagnosticAnalyzer
	{
		public const string ScriptableObjectId = "UNT0011";

		public static readonly DiagnosticDescriptor ScriptableObjectRule = new DiagnosticDescriptor(
			ScriptableObjectId,
			title: Strings.CreateScriptableObjectInstanceDiagnosticTitle,
			messageFormat: Strings.CreateScriptableObjectInstanceDiagnosticMessageFormat,
			category: DiagnosticCategory.TypeSafety,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.CreateScriptableObjectInstanceDiagnosticDescription);

		public const string MonoBehaviourId = "UNT0010";

		public static readonly DiagnosticDescriptor MonoBehaviourIdRule = new DiagnosticDescriptor(
			MonoBehaviourId,
			title: Strings.CreateMonoBehaviourInstanceDiagnosticTitle,
			messageFormat: Strings.CreateMonoBehaviourInstanceDiagnosticMessageFormat,
			category: DiagnosticCategory.TypeSafety,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.CreateMonoBehaviourInstanceDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ScriptableObjectRule, MonoBehaviourIdRule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
		}

		private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
		{
			if (!(context.Node is ObjectCreationExpressionSyntax creation))
				return;

			var symbolInfo = ModelExtensions.GetSymbolInfo(context.SemanticModel, creation);
			var typeSymbol = symbolInfo.Symbol.ContainingType;

			var scriptInfo = new ScriptInfo(typeSymbol);
			if (scriptInfo.Metadata == typeof(ScriptableObject))
				context.ReportDiagnostic(Diagnostic.Create(ScriptableObjectRule, creation.GetLocation(), typeSymbol.Name));

			// For MonoBehaviour, we have to check that the current class is an Unity Component, so gameObject field is available
			if (scriptInfo.Metadata != typeof(MonoBehaviour))
				return;

			var classDeclarationSyntax = creation
				.Ancestors()
				.OfType<ClassDeclarationSyntax>()
				.FirstOrDefault();

			if (classDeclarationSyntax == null)
				return;

			var classSymbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclarationSyntax) as ITypeSymbol;
			var componentSymbol = context.Compilation.GetTypeByMetadataName("UnityEngine.Component");

			if (InheritsFrom(classSymbol, componentSymbol))
				context.ReportDiagnostic(Diagnostic.Create(MonoBehaviourIdRule, creation.GetLocation(), typeSymbol.Name));
		}

		private static bool InheritsFrom(ITypeSymbol symbol, ITypeSymbol type)
		{
			if (symbol == null || type == null)
				return false;

			var baseType = symbol.BaseType;
			while (baseType != null)
			{
				if (type.Equals(baseType))
					return true;

				baseType = baseType.BaseType;
			}

			return false;
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class CreateInstanceCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CreateInstanceAnalyzer.MonoBehaviourId, CreateInstanceAnalyzer.ScriptableObjectId);
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root.FindNode(context.Span) is ObjectCreationExpressionSyntax creation))
				return;

			var id = context.Diagnostics.Select(d => d.Id).FirstOrDefault();

			Func<CancellationToken, Task<Document>> replacer;
			if (id == CreateInstanceAnalyzer.ScriptableObjectId)
				replacer = ct => ReplaceWithInvocationAsync(context.Document, creation, "ScriptableObject", "CreateInstance", ct);
			else
				replacer = ct => ReplaceWithInvocationAsync(context.Document, creation, "gameObject", "AddComponent", ct);

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.CreateScriptableObjectInstanceCodeFixTitle,
					replacer,
					creation.ToFullString()),
				context.Diagnostics);
		}

		protected async Task<Document> ReplaceWithInvocationAsync(Document document, ObjectCreationExpressionSyntax creation, string identifierName, string genericMethodName, CancellationToken ct)
		{
			var root = await document
				.GetSyntaxRootAsync(ct)
				.ConfigureAwait(false);

			var semanticModel = await document
				.GetSemanticModelAsync(ct)
				.ConfigureAwait(false);

			var symbolInfo = ModelExtensions.GetSymbolInfo(semanticModel, creation);
			var typeSymbol = symbolInfo.Symbol.ContainingType;

			var invocation = SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					SyntaxFactory.IdentifierName(identifierName),
					SyntaxFactory.GenericName(SyntaxFactory.Identifier(genericMethodName))
						.WithTypeArgumentList(
							SyntaxFactory.TypeArgumentList(
								SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
									SyntaxFactory.IdentifierName(typeSymbol.Name))))));

			var newRoot = root.ReplaceNode(creation, invocation);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
