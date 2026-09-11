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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ShaderPropertyToIDAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0046";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.ShaderPropertyToIDDiagnosticTitle,
		messageFormat: Strings.ShaderPropertyToIDDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.ShaderPropertyToIDDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;
		var name = invocation.Expression switch
		{
			MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
			IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
			_ => null
		};

		if (name != "PropertyToID" || invocation.ArgumentList.Arguments.Count != 1
			|| invocation.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax literal
			|| !literal.IsKind(SyntaxKind.StringLiteralExpression)
			|| invocation.ContainsDirectives)
			return;

		for (var parent = invocation.Parent; parent != null; parent = parent.Parent)
		{
			// Initializers already cache the computed ID.
			if (parent is BaseFieldDeclarationSyntax or EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax })
				return;

			if (parent is MemberDeclarationSyntax)
				break;
		}

		if (context.SemanticModel.GetOperation(invocation, context.CancellationToken) is not IInvocationOperation operation
			|| operation.Parent is IExpressionStatementOperation)
			return;

		var method = operation.TargetMethod;
		if (!method.IsStatic || !method.ContainingType.Matches(typeof(UnityEngine.Shader))
			|| method.Parameters.Length != 1
			|| method.Parameters[0].Type.SpecialType != SpecialType.System_String
			|| method.ReturnType.SpecialType != SpecialType.System_Int32)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), literal.Token.ValueText));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class ShaderPropertyToIDCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ShaderPropertyToIDAnalyzer.Rule.Id);

	// Independent batch actions cannot coordinate generated fields in the same type.
	public sealed override FixAllProvider? GetFixAllProvider() => null;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var invocation = await context.GetFixableNodeAsync<InvocationExpressionSyntax>();
		if (invocation == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.ShaderPropertyToIDCodeFixTitle,
				ct => CachePropertyIdAsync(context.Document, invocation, ct),
				ShaderPropertyToIDAnalyzer.Rule.Id),
			context.Diagnostics);
	}

	private static async Task<Document> CachePropertyIdAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
	{
		var declaration = invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>();
		var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (declaration == null || model?.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol factory)
			return document;

		if (invocation.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax literal)
			return document;

		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
		var fieldName = CachedStringIdField.GetOrCreate(editor, declaration, factory, literal.Token.ValueText, "Id", invocation, cancellationToken);
		if (fieldName == null)
			return document;

		var interiorTrivia = invocation.DescendantTrivia().Where(trivia => invocation.Span.Contains(trivia.Span));
		var reference = IdentifierName(fieldName)
			.WithTriviaFrom(invocation)
			.WithLeadingTrivia(invocation.GetLeadingTrivia().AddRange(interiorTrivia))
			.WithAdditionalAnnotations(Formatter.Annotation);
		editor.ReplaceNode(invocation, reference);

		return editor.GetChangedDocument();
	}
}
