/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NonAllocatingArrayAccessAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0045";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.NonAllocatingArrayAccessDiagnosticTitle,
		messageFormat: Strings.NonAllocatingArrayAccessDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.NonAllocatingArrayAccessDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	private static readonly Dictionary<string, (Type[] Types, string Count, string Element)> Candidates = new()
	{
		["contacts"] = ([typeof(UnityEngine.Collision), typeof(UnityEngine.Collision2D)], "contactCount", "GetContact"),
		["touches"] = ([typeof(UnityEngine.Input)], "touchCount", "GetTouch"),
		["accelerationEvents"] = ([typeof(UnityEngine.Input)], "accelerationEventCount", "GetAccelerationEvent")
	};

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeAccess, SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.ElementAccessExpression);
	}

	private static void AnalyzeAccess(SyntaxNodeAnalysisContext context)
	{
		if (!TryGetAccess(context.Node, out var arrayAccess, out var candidate))
			return;

		var model = context.SemanticModel;
		if (model.GetSymbolInfo(arrayAccess, context.CancellationToken).Symbol is not IPropertySymbol property
			|| property.Type is not IArrayTypeSymbol arrayType
			|| !IsCandidateType(property.ContainingType, candidate.Types))
			return;

		if (!SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(arrayAccess.Expression, context.CancellationToken).Type, property.ContainingType))
			return;

		if (context.Node is ElementAccessExpressionSyntax element)
		{
			if (model.GetTypeInfo(element.ArgumentList.Arguments[0].Expression, context.CancellationToken).ConvertedType?.SpecialType != SpecialType.System_Int32)
				return;

			if (model.GetOperation(element, context.CancellationToken)?.Parent is IArgumentOperation { Parameter.RefKind: not RefKind.None })
				return;
		}

		if (!HasReplacement(property, arrayType.ElementType, candidate.Member, context.Node is ElementAccessExpressionSyntax))
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), candidate.Member, arrayAccess.Name.Identifier.ValueText));
	}

	internal static bool TryGetAccess(SyntaxNode node, [NotNullWhen(true)] out MemberAccessExpressionSyntax? arrayAccess,
		out (Type[] Types, string Member) candidate)
	{
		arrayAccess = null;
		candidate = default;

		var expression = node switch
		{
			MemberAccessExpressionSyntax member when member.Name.Identifier.ValueText == nameof(Array.Length) => member.Expression,
			ElementAccessExpressionSyntax element when element.ArgumentList.Arguments.Count == 1 => element.Expression,
			_ => null
		};

		if (expression is not MemberAccessExpressionSyntax memberAccess
			|| !Candidates.TryGetValue(memberAccess.Name.Identifier.ValueText, out var entry))
			return false;

		if (node.ContainsDirectives || IsInsideNameOf(node))
			return false;

		if (node is ElementAccessExpressionSyntax indexer)
		{
			if (!IsElementRead(indexer))
				return false;

			foreach (var descendant in indexer.ArgumentList.Arguments[0].Expression.DescendantNodesAndSelf())
			{
				if (descendant is AwaitExpressionSyntax)
					return false;
			}
		}

		arrayAccess = memberAccess;
		candidate = (entry.Types, node is ElementAccessExpressionSyntax ? entry.Element : entry.Count);
		return true;
	}

	private static bool IsCandidateType(ITypeSymbol type, Type[] candidates)
	{
		foreach (var candidate in candidates)
		{
			if (type.Matches(candidate))
				return true;
		}

		return false;
	}

	private static bool HasReplacement(IPropertySymbol property, ITypeSymbol elementType, string name, bool isElement)
	{
		foreach (var member in property.ContainingType.GetMembers(name))
		{
			if (member.IsStatic != property.IsStatic)
				continue;

			if (!isElement && member is IPropertySymbol { IsIndexer: false } count
				&& count.Type.SpecialType == SpecialType.System_Int32
				&& count.GetMethod?.DeclaredAccessibility == Accessibility.Public)
				return true;

			if (isElement && member is IMethodSymbol method
				&& method.DeclaredAccessibility == Accessibility.Public
				&& method.Parameters.Length == 1
				&& method.Parameters[0].RefKind == RefKind.None
				&& method.Parameters[0].Type.SpecialType == SpecialType.System_Int32
				&& SymbolEqualityComparer.Default.Equals(method.ReturnType, elementType))
				return true;
		}

		return false;
	}

	private static bool IsInsideNameOf(SyntaxNode node)
	{
		for (var parent = node.Parent; parent != null; parent = parent.Parent)
		{
			if (parent is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } })
				return true;
		}

		return false;
	}

	private static bool IsElementRead(ElementAccessExpressionSyntax element)
	{
		SyntaxNode current = element;
		while (true)
		{
			switch (current.Parent)
			{
				case ParenthesizedExpressionSyntax parentheses:
					current = parentheses;
					continue;
				case PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression):
					current = postfix;
					continue;
				case MemberAccessExpressionSyntax member when member.Expression == current:
					current = member;
					continue;
				case ArgumentSyntax { Parent: TupleExpressionSyntax tuple }:
					current = tuple;
					continue;
			}

			break;
		}

		// Array elements are variables; getter results are values.
		return current.Parent switch
		{
			AssignmentExpressionSyntax assignment when assignment.Left == current => false,
			ArgumentSyntax argument when !argument.RefKindKeyword.IsKind(SyntaxKind.None) => false,
			RefExpressionSyntax => false,
			PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.PreIncrementExpression)
				|| prefix.IsKind(SyntaxKind.PreDecrementExpression) || prefix.IsKind(SyntaxKind.AddressOfExpression) => false,
			PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.PostIncrementExpression)
				|| postfix.IsKind(SyntaxKind.PostDecrementExpression) => false,
			InvocationExpressionSyntax invocation when invocation.Expression == current => false,
			_ => true
		};
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class NonAllocatingArrayAccessCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NonAllocatingArrayAccessAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var access = await context.GetFixableNodeAsync<ExpressionSyntax>();
		if (access == null || !NonAllocatingArrayAccessAnalyzer.TryGetAccess(access, out var arrayAccess, out var candidate))
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.NonAllocatingArrayAccessCodeFixTitle,
				ct => ReplaceAccessAsync(context.Document, access, arrayAccess, candidate.Member, ct),
				NonAllocatingArrayAccessAnalyzer.Rule.Id),
			context.Diagnostics);
	}

	private static async Task<Document> ReplaceAccessAsync(Document document, ExpressionSyntax access,
		MemberAccessExpressionSyntax arrayAccess, string replacementName, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root == null)
			return document;

		ExpressionSyntax replacement = arrayAccess.WithName(IdentifierName(replacementName).WithTriviaFrom(arrayAccess.Name));
		if (access is ElementAccessExpressionSyntax element)
		{
			replacement = InvocationExpression(replacement,
				ArgumentList(
					Token(SyntaxKind.OpenParenToken).WithTriviaFrom(element.ArgumentList.OpenBracketToken),
					element.ArgumentList.Arguments,
					Token(SyntaxKind.CloseParenToken).WithTriviaFrom(element.ArgumentList.CloseBracketToken)))
				.WithTriviaFrom(access);
		}
		else if (access is MemberAccessExpressionSyntax length)
		{
			var trailingTrivia = arrayAccess.GetTrailingTrivia()
				.AddRange(length.OperatorToken.LeadingTrivia)
				.AddRange(length.OperatorToken.TrailingTrivia)
				.AddRange(length.Name.GetLeadingTrivia())
				.AddRange(length.GetTrailingTrivia());

			replacement = replacement.WithLeadingTrivia(access.GetLeadingTrivia()).WithTrailingTrivia(trailingTrivia);
		}

		return document.WithSyntaxRoot(root.ReplaceNode(access, replacement));
	}
}
