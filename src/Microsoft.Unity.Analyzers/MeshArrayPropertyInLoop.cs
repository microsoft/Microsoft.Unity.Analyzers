/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
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
public class MeshArrayPropertyInLoopAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0042";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.MeshArrayPropertyInLoopDiagnosticTitle,
		messageFormat: Strings.MeshArrayPropertyInLoopDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.MeshArrayPropertyInLoopDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	private static readonly Type[] AllocatingArrayElementTypes =
	[
		typeof(UnityEngine.Vector2), typeof(UnityEngine.Vector3), typeof(UnityEngine.Vector4), typeof(UnityEngine.Color), typeof(UnityEngine.Color32)
	];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
	}

	private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not MemberAccessExpressionSyntax memberAccess)
			return;

		var loop = FindContainingLoop(memberAccess);
		if (loop == null)
			return;

		if (!IsAllocatingMeshProperty(context, memberAccess, out var propertyName))
			return;

		// Only report if this is the first occurrence of this property access in the loop
		// to avoid flooding the user with multiple diagnostics for the same issue
		if (!IsFirstOccurrenceInLoop(context.SemanticModel, memberAccess, loop))
			return;

		context.ReportDiagnostic(Diagnostic.Create(
			Rule,
			memberAccess.GetLocation(),
			propertyName));
	}

	internal static SyntaxNode? FindContainingLoop(SyntaxNode node)
	{
		var current = node.Parent;
		while (current != null)
		{
			switch (current)
			{
				case ForStatementSyntax:
				case ForEachStatementSyntax:
				case WhileStatementSyntax:
				case DoStatementSyntax:
					return current;

				// Stop looking if we hit a method/lambda/function boundary
				case MethodDeclarationSyntax:
				case LocalFunctionStatementSyntax:
				case AnonymousFunctionExpressionSyntax:
					return null;
			}

			current = current.Parent;
		}

		return null;
	}

	private static bool IsFirstOccurrenceInLoop(SemanticModel model, MemberAccessExpressionSyntax memberAccess, SyntaxNode loop)
	{
		var allOccurrences = loop.DescendantNodes()
			.OfType<MemberAccessExpressionSyntax>()
			.Where(m => IsInLoopConditionOrBody(m, loop) && IsSameMeshPropertyAccess(model, m, memberAccess))
			.OrderBy(m => m.SpanStart)
			.ToList();

		return allOccurrences.Count > 0 && allOccurrences[0] == memberAccess;
	}

	internal static bool IsSameMeshPropertyAccess(SemanticModel model, MemberAccessExpressionSyntax candidate, MemberAccessExpressionSyntax reference)
	{
		var candidateSymbol = model.GetSymbolInfo(candidate).Symbol;
		var referenceSymbol = model.GetSymbolInfo(reference).Symbol;

		if (candidateSymbol == null || referenceSymbol == null)
			return false;

		if (!SymbolEqualityComparer.Default.Equals(candidateSymbol, referenceSymbol))
			return false;

		var candidateExprSymbol = model.GetSymbolInfo(candidate.Expression).Symbol;
		var referenceExprSymbol = model.GetSymbolInfo(reference.Expression).Symbol;

		return SymbolEqualityComparer.Default.Equals(candidateExprSymbol, referenceExprSymbol);
	}

	internal static bool IsInLoopConditionOrBody(SyntaxNode node, SyntaxNode loop)
	{
		if (!loop.Contains(node))
			return false;

		var current = node.Parent;
		while (current != null && current != loop)
		{
			switch (current)
			{
				case MethodDeclarationSyntax:
				case LocalFunctionStatementSyntax:
				case AnonymousFunctionExpressionSyntax:
					return false;
			}

			current = current.Parent;
		}

		return loop switch
		{
			ForStatementSyntax forStatement =>
				IsInLoopConditionOrIncrementors(node, forStatement) || forStatement.Statement.Contains(node),
			ForEachStatementSyntax forEachStatement =>
				forEachStatement.Statement.Contains(node),
			WhileStatementSyntax whileStatement =>
				whileStatement.Condition.Contains(node) || whileStatement.Statement.Contains(node),
			DoStatementSyntax doStatement =>
				doStatement.Condition.Contains(node) || doStatement.Statement.Contains(node),
			_ => false
		};
	}

	private static bool IsInLoopConditionOrIncrementors(SyntaxNode node, ForStatementSyntax forStatement)
	{
		if (forStatement.Condition != null && forStatement.Condition.Contains(node))
			return true;

		foreach (var incrementor in forStatement.Incrementors)
		{
			if (incrementor.Contains(node))
				return true;
		}

		return false;
	}

	internal static bool IsAllocatingMeshProperty(SyntaxNodeAnalysisContext context, MemberAccessExpressionSyntax memberAccess, out string? propertyName)
	{
		return IsAllocatingMeshProperty(context.SemanticModel, memberAccess, out propertyName);
	}

	internal static bool IsAllocatingMeshProperty(SemanticModel model, MemberAccessExpressionSyntax memberAccess, out string? propertyName)
	{
		propertyName = null;

		var symbol = model.GetSymbolInfo(memberAccess).Symbol;
		if (symbol is not IPropertySymbol propertySymbol)
			return false;

		var containingType = propertySymbol.ContainingType;
		if (!containingType.Matches(typeof(UnityEngine.Mesh)))
			return false;

		if (propertySymbol.Type is not IArrayTypeSymbol arrayType)
			return false;

		var element = arrayType.ElementType;
		if (AllocatingArrayElementTypes.All(t => !element.Matches(t)))
			return false;

		propertyName = propertySymbol.Name;
		return true;
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class MeshArrayPropertyInLoopCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MeshArrayPropertyInLoopAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var memberAccess = await context.GetFixableNodeAsync<MemberAccessExpressionSyntax>();
		if (memberAccess == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.MeshArrayPropertyInLoopCodeFixTitle,
				ct => CacheMeshPropertyAsync(context.Document, memberAccess, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Document> CacheMeshPropertyAsync(Document document, MemberAccessExpressionSyntax memberAccess, CancellationToken cancellationToken)
	{
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (semanticModel == null)
			return document;

		var symbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
		if (symbol is not IPropertySymbol propertySymbol)
			return document;

		var loop = MeshArrayPropertyInLoopAnalyzer.FindContainingLoop(memberAccess);
		if (loop == null)
			return document;

		var allOccurrences = FindAllPropertyAccesses(semanticModel, memberAccess, loop);
		var variableName = GenerateVariableName(semanticModel, memberAccess, propertySymbol, loop);
		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
		var typeName = propertySymbol.Type.ToMinimalDisplayString(semanticModel, memberAccess.SpanStart);

		var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
			SyntaxFactory.VariableDeclaration(
				SyntaxFactory.ParseTypeName(typeName),
				SyntaxFactory.SingletonSeparatedList(
					SyntaxFactory.VariableDeclarator(
						SyntaxFactory.Identifier(variableName),
						null,
						SyntaxFactory.EqualsValueClause(memberAccess.WithoutTrivia())))
			)
		).NormalizeWhitespace();

		var leadingTrivia = loop.GetLeadingTrivia();
		var indentation = leadingTrivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
		variableDeclaration = variableDeclaration
			.WithLeadingTrivia(indentation.IsKind(SyntaxKind.WhitespaceTrivia) ? SyntaxFactory.TriviaList(indentation) : leadingTrivia)
			.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

		editor.InsertBefore(loop, variableDeclaration);

		foreach (var occurrence in allOccurrences)
			editor.ReplaceNode(occurrence, SyntaxFactory.IdentifierName(variableName).WithTriviaFrom(occurrence));

		return editor.GetChangedDocument();
	}

	private static List<MemberAccessExpressionSyntax> FindAllPropertyAccesses(SemanticModel model, MemberAccessExpressionSyntax memberAccess, SyntaxNode loop)
	{
		return [.. loop.DescendantNodes()
			.OfType<MemberAccessExpressionSyntax>()
			.Where(m => MeshArrayPropertyInLoopAnalyzer.IsSameMeshPropertyAccess(model, m, memberAccess) && MeshArrayPropertyInLoopAnalyzer.IsInLoopConditionOrBody(m, loop))];
	}

	private static string GenerateVariableName(SemanticModel semanticModel, MemberAccessExpressionSyntax memberAccess, IPropertySymbol propertySymbol, SyntaxNode loop)
	{
		var prefix = memberAccess.Expression switch
		{
			IdentifierNameSyntax identifier => identifier.Identifier.Text,
			MemberAccessExpressionSyntax mae => mae.Name.Identifier.Text,
			_ => "mesh"
		};

		var baseName = $"{prefix}{char.ToUpperInvariant(propertySymbol.Name[0])}{propertySymbol.Name.Substring(1)}";
		var candidateName = baseName;
		var counter = 1;

		while (IsNameInScope(semanticModel, loop, candidateName))
		{
			candidateName = $"{baseName}{counter}";
			counter++;
		}

		return candidateName;
	}

	private static bool IsNameInScope(SemanticModel semanticModel, SyntaxNode location, string name)
	{
		var symbols = semanticModel.LookupSymbols(location.SpanStart, name: name);
		return symbols.Length > 0;
	}
}
