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

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class CreateInstanceAnalyzer : DiagnosticAnalyzer
	{
		public const string MonoBehaviourId = "UNT0010";

		public static readonly DiagnosticDescriptor MonoBehaviourIdRule = new DiagnosticDescriptor(
			MonoBehaviourId,
			title: Strings.CreateMonoBehaviourInstanceDiagnosticTitle,
			messageFormat: Strings.CreateMonoBehaviourInstanceDiagnosticMessageFormat,
			category: DiagnosticCategory.TypeSafety,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.CreateMonoBehaviourInstanceDiagnosticDescription);

		public const string ScriptableObjectId = "UNT0011";

		public static readonly DiagnosticDescriptor ScriptableObjectRule = new DiagnosticDescriptor(
			ScriptableObjectId,
			title: Strings.CreateScriptableObjectInstanceDiagnosticTitle,
			messageFormat: Strings.CreateScriptableObjectInstanceDiagnosticMessageFormat,
			category: DiagnosticCategory.TypeSafety,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.CreateScriptableObjectInstanceDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MonoBehaviourIdRule, ScriptableObjectRule);

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

			var typeInfo = context.SemanticModel.GetTypeInfo(creation);
			if (typeInfo.Type.Extends(typeof(UnityEngine.ScriptableObject)))
			{
				context.ReportDiagnostic(Diagnostic.Create(ScriptableObjectRule, creation.GetLocation(), typeInfo.Type.Name));
				return;
			}

			if (!typeInfo.Type.Extends(typeof(UnityEngine.MonoBehaviour)))
				return;

			context.ReportDiagnostic(Diagnostic.Create(MonoBehaviourIdRule, creation.GetLocation(), typeInfo.Type.Name));
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
			var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root.FindNode(context.Span) is ObjectCreationExpressionSyntax creation))
				return;

			var diagnostic = context.Diagnostics.FirstOrDefault();
			if (diagnostic == null)
				return;

			switch (diagnostic.Id)
			{
				case CreateInstanceAnalyzer.ScriptableObjectId:
					context.RegisterCodeFix(
						CodeAction.Create(
							Strings.CreateScriptableObjectInstanceCodeFixTitle,
							ct => ReplaceWithInvocationAsync(context.Document, creation, "ScriptableObject", "CreateInstance", ct),
							creation.ToFullString()),
						context.Diagnostics);
					break;
				case CreateInstanceAnalyzer.MonoBehaviourId when !IsInsideComponent(creation, model):
					return;
				case CreateInstanceAnalyzer.MonoBehaviourId:
					context.RegisterCodeFix(
						CodeAction.Create(
							Strings.CreateMonoBehaviourInstanceCodeFixTitle,
							ct => ReplaceWithInvocationAsync(context.Document, creation, "gameObject", "AddComponent", ct),
							creation.ToFullString()),
						context.Diagnostics);
					break;
			}
		}

		private static bool IsInsideComponent(ObjectCreationExpressionSyntax creation, SemanticModel model)
		{
			var classDeclaration = creation
				.Ancestors()
				.OfType<ClassDeclarationSyntax>()
				.FirstOrDefault();

			if (classDeclaration == null)
				return false;

			var symbol = model.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
			return symbol.Extends(typeof(UnityEngine.Component));
		}

		private async Task<Document> ReplaceWithInvocationAsync(Document document, ObjectCreationExpressionSyntax creation, string identifierName, string genericMethodName, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

			var typeInfo = semanticModel.GetTypeInfo(creation);

			var invocation = InvocationExpression(
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					IdentifierName(identifierName),
					GenericName(Identifier(genericMethodName))
						.WithTypeArgumentList(
							TypeArgumentList(
								SingletonSeparatedList<TypeSyntax>(
									IdentifierName(typeInfo.Type.Name))))));

			var newRoot = root.ReplaceNode(creation, invocation);
			return document.WithSyntaxRoot(newRoot);
		}
	}
}
