/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
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

		var fieldName = GenerateFieldName(literalValue);

		var fieldExists = classDecl.Members
			.OfType<FieldDeclarationSyntax>()
			.SelectMany(f => f.Declaration.Variables)
			.Any(v => v.Identifier.Text == fieldName);

		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

		if (!fieldExists)
		{
			// Create: private static readonly int FieldName = Animator.StringToHash("value");
			var hashInvocation = SyntaxFactory.InvocationExpression(
					SyntaxFactory.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						SyntaxFactory.IdentifierName(nameof(UnityEngine.Animator)),
						SyntaxFactory.IdentifierName("StringToHash")))
				.WithArgumentList(SyntaxFactory.ArgumentList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.Argument(
							SyntaxFactory.LiteralExpression(
								SyntaxKind.StringLiteralExpression,
								SyntaxFactory.Literal(literalValue))))));

			var fieldDecl = SyntaxFactory.FieldDeclaration(
					SyntaxFactory.VariableDeclaration(
						SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
						SyntaxFactory.SeparatedList([
							SyntaxFactory.VariableDeclarator(
								SyntaxFactory.Identifier(fieldName),
								null,
								SyntaxFactory.EqualsValueClause(hashInvocation))
						])))
				.AddModifiers(
					SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
					SyntaxFactory.Token(SyntaxKind.StaticKeyword),
					SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

			editor.InsertMembers(classDecl, 0, [fieldDecl]);
		}

		var newArgument = stringArgument
			.WithExpression(SyntaxFactory.IdentifierName(fieldName))
			.WithTrailingTrivia(stringArgument.GetTrailingTrivia());

		editor.ReplaceNode(stringArgument, newArgument);

		return editor.GetChangedDocument();
	}

	private static string GenerateFieldName(string literalValue)
	{
		var cleaned = Regex.Replace(literalValue, @"[^a-zA-Z0-9]", " ");

		var words = cleaned.Split([' '], StringSplitOptions.RemoveEmptyEntries);
		var pascalCase = string.Concat(words.Select(ToPascalCaseWord));

		if (!string.IsNullOrEmpty(pascalCase) && char.IsDigit(pascalCase[0]))
			pascalCase = "_" + pascalCase;

		return pascalCase + "Hash";
	}

	private static string ToPascalCaseWord(string word)
	{
		if (string.IsNullOrEmpty(word))
			return "";

		return char.ToUpperInvariant(word[0]) + word.Substring(1);
	}
}
