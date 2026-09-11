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
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AnimatorStringToHashAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0041";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.AnimatorStringToHashDiagnosticTitle,
		messageFormat: Strings.AnimatorStringToHashDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.AnimatorStringToHashDiagnosticDescription);

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

		if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
			return;

		var containingType = methodSymbol.ContainingType;

		if (containingType == null || !containingType.Matches(typeof(UnityEngine.Animator)))
			return;

		var stringArgument = FindStringArgumentWithHashOverload(invocation, methodSymbol);
		if (stringArgument?.Expression is not LiteralExpressionSyntax literal || !literal.IsKind(SyntaxKind.StringLiteralExpression))
			return;

		context.ReportDiagnostic(Diagnostic.Create(
			Rule,
			invocation.GetLocation(),
			methodSymbol.Name,
			literal.Token.ValueText));
	}

	internal static ArgumentSyntax? FindStringArgumentWithHashOverload(InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol)
	{
		var arguments = invocation.ArgumentList.Arguments;

		for (var i = 0; i < methodSymbol.Parameters.Length && i < arguments.Count; i++)
		{
			var parameter = methodSymbol.Parameters[i];
			if (!parameter.Type.Matches(typeof(string)))
				continue;

			if (HasIntOverloadAtPosition(methodSymbol, i))
				return arguments[i];
		}

		return null;
	}

	internal static bool HasIntOverloadAtPosition(IMethodSymbol methodSymbol, int stringParameterIndex)
	{
		var containingType = methodSymbol.ContainingType;
		var overloads = containingType.GetMembers(methodSymbol.Name)
			.OfType<IMethodSymbol>()
			.Where(m => m.Parameters.Length == methodSymbol.Parameters.Length);

		return overloads
			.Where(overload => !SymbolEqualityComparer.Default.Equals(overload, methodSymbol))
			.Any(overload => overload.Parameters.Select((param, i) => i == stringParameterIndex
					? param.Type.SpecialType == SpecialType.System_Int32
					: SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[i].Type, param.Type))
				.All(match => match));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class AnimatorStringToHashCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AnimatorStringToHashAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var invocation = await context.GetFixableNodeAsync<InvocationExpressionSyntax>();
		if (invocation == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.AnimatorStringToHashCodeFixTitle,
				ct => ExtractToHashFieldAsync(context.Document, invocation, ct),
				FixableDiagnosticIds.Single()),
			context.Diagnostics);
	}

	private static async Task<Document> ExtractToHashFieldAsync(
		Document document,
		InvocationExpressionSyntax invocation,
		CancellationToken cancellationToken)
	{
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (semanticModel?.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
			return document;

		var stringArgument = AnimatorStringToHashAnalyzer.FindStringArgumentWithHashOverload(invocation, methodSymbol);
		if (stringArgument == null)
			return document;

		var literalValue = (stringArgument.Expression as LiteralExpressionSyntax)?.Token.ValueText;
		if (literalValue == null)
			return document;

		var classDecl = invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>();
		if (classDecl == null)
			return document;

		var factory = methodSymbol.ContainingType.GetMembers("StringToHash")
			.OfType<IMethodSymbol>()
			.FirstOrDefault(m => m.IsStatic && m.Parameters.Length == 1
				&& m.Parameters[0].Type.SpecialType == SpecialType.System_String
				&& m.ReturnType.SpecialType == SpecialType.System_Int32);
		if (factory == null)
			return document;

		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
		var fieldName = CachedStringIdField.GetOrCreate(editor, classDecl, factory, literalValue, "Hash",
			stringArgument.Expression, cancellationToken);
		if (fieldName == null)
			return document;

		var newArgument = stringArgument
			.WithExpression(SyntaxFactory.IdentifierName(fieldName).WithTriviaFrom(stringArgument.Expression));

		editor.ReplaceNode(stringArgument, newArgument);

		return editor.GetChangedDocument();
	}
}
