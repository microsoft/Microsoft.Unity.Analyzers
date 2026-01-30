/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

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

	// Array element types that cause allocations when returned from Mesh properties
	private static readonly Type[] AllocatingArrayElementTypes =
	[
		typeof(UnityEngine.Vector2),
		typeof(UnityEngine.Vector3),
		typeof(UnityEngine.Vector4),
		typeof(UnityEngine.Color),
		typeof(UnityEngine.Color32)
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

		// Check if this is inside a loop
		if (!IsInsideLoop(memberAccess))
			return;

		// Check if accessing an allocating property on Mesh
		if (!IsAllocatingMeshProperty(context, memberAccess, out var propertyName))
			return;

		context.ReportDiagnostic(Diagnostic.Create(
			Rule,
			memberAccess.GetLocation(),
			propertyName));
	}

	private static bool IsInsideLoop(SyntaxNode node)
	{
		var current = node.Parent;
		while (current != null)
		{
			switch (current)
			{
				case ForStatementSyntax forStatement:
					// Check if the node is in the condition or incrementors (these are evaluated each iteration)
					if (IsInLoopConditionOrIncrementors(node, forStatement))
						return true;
					// Check if the node is in the body
					if (forStatement.Statement != null && forStatement.Statement.Contains(node))
						return true;
					break;

				case ForEachStatementSyntax forEachStatement:
					if (forEachStatement.Statement != null && forEachStatement.Statement.Contains(node))
						return true;
					break;

				case WhileStatementSyntax whileStatement:
					// Condition is evaluated each iteration
					if (whileStatement.Condition.Contains(node))
						return true;
					if (whileStatement.Statement != null && whileStatement.Statement.Contains(node))
						return true;
					break;

				case DoStatementSyntax doStatement:
					// Condition is evaluated each iteration
					if (doStatement.Condition.Contains(node))
						return true;
					if (doStatement.Statement != null && doStatement.Statement.Contains(node))
						return true;
					break;

				// Stop looking if we hit a method/lambda/function boundary
				case MethodDeclarationSyntax:
				case LocalFunctionStatementSyntax:
				case AnonymousFunctionExpressionSyntax:
					return false;
			}

			current = current.Parent;
		}

		return false;
	}

	private static bool IsInLoopConditionOrIncrementors(SyntaxNode node, ForStatementSyntax forStatement)
	{
		// Check if node is in the condition
		if (forStatement.Condition != null && forStatement.Condition.Contains(node))
			return true;

		// Check if node is in any of the incrementors
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

		// Check if it's a Mesh property
		var containingType = propertySymbol.ContainingType;
		if (containingType == null || containingType.Name != "Mesh" || containingType.ContainingNamespace?.ToDisplayString() != "UnityEngine")
			return false;

		// Check if the return type is an array of one of the allocating element types
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

		// Find the containing loop
		var loop = FindContainingLoop(memberAccess);
		if (loop == null)
			return document;

		// Generate a variable name based on the property
		var variableName = GenerateVariableName(memberAccess, propertySymbol);

		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

		// Create the variable declaration
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

		// Preserve the leading trivia (indentation) from the loop
		var leadingTrivia = loop.GetLeadingTrivia();
		variableDeclaration = variableDeclaration
			.WithLeadingTrivia(leadingTrivia)
			.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

		// Insert the declaration before the loop
		editor.InsertBefore(loop, variableDeclaration);

		// Replace the member access with the variable reference
		editor.ReplaceNode(memberAccess, SyntaxFactory.IdentifierName(variableName).WithTriviaFrom(memberAccess));

		return editor.GetChangedDocument();
	}

	private static SyntaxNode? FindContainingLoop(SyntaxNode node)
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

	private static string GenerateVariableName(MemberAccessExpressionSyntax memberAccess, IPropertySymbol propertySymbol)
	{
		// Try to get a meaningful prefix from the expression (e.g., "mesh" from "mesh.vertices")
		var prefix = memberAccess.Expression switch
		{
			IdentifierNameSyntax identifier => identifier.Identifier.Text,
			MemberAccessExpressionSyntax mae => mae.Name.Identifier.Text,
			_ => "mesh"
		};

		// Combine prefix with property name
		return $"{prefix}{char.ToUpperInvariant(propertySymbol.Name[0])}{propertySymbol.Name.Substring(1)}";
	}
}
