/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TryGetComponentAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0026";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.TryGetComponentDiagnosticTitle,
		messageFormat: Strings.TryGetComponentDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.TryGetComponentDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		if (!IsTryGetComponentSupported(context))
			return;

		var invocation = (InvocationExpressionSyntax)context.Node;
		var tgcContext = TryGetComponentContext.GetContext(invocation, context.SemanticModel);
		if (tgcContext == null)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
	}

	private static bool IsTryGetComponentSupported(SyntaxNodeAnalysisContext context)
	{
		// We need Unity 2019.2+ for proper support
		var goType = context.Compilation?.GetTypeByMetadataName(typeof(UnityEngine.GameObject).FullName!);
		return goType?.MemberNames.Contains(nameof(UnityEngine.Component.TryGetComponent)) ?? false;
	}
}

internal class TryGetComponentContext(string targetIdentifier, bool isVariableDeclaration, IfStatementSyntax ifStatement, SyntaxKind conditionKind)
{
	public string TargetIdentifier { get; } = targetIdentifier;
	public bool IsVariableDeclaration { get; } = isVariableDeclaration;
	public IfStatementSyntax IfStatement { get; } = ifStatement;
	public SyntaxKind ConditionKind { get; } = conditionKind;

	public static TryGetComponentContext? GetContext(InvocationExpressionSyntax invocation, SemanticModel model)
	{
		// We want the generic GetComponent, no arguments
		if (!IsCompatibleGetComponent(invocation, model))
			return null;

		// We want an assignment or variable declaration with invocation as initializer
		var invocationParent = invocation.Parent;
		if (invocationParent == null)
			return null;

		if (!TryGetTargetdentifier(model, invocationParent, out var targetIdentifier, out var isVariableDeclaration))
			return null;

		// We want the next line to be an if statement
		if (!TryGetNextTopNode(invocationParent, out var ifNode))
			return null;

		// We want a binary expression
		if (!TryGetConditionIdentifier(ifNode, out var conditionIdentifier, out var binaryExpression, out var ifStatement))
			return null;

		// Reusing the same identifier
		if (conditionIdentifier.Value.Text != targetIdentifier.Value.Text)
			return null;

		// We allow inline ifs without else clause
		var visitor = invocationParent;
		while (visitor != null)
		{
			if (visitor is BlockSyntax)
				break;

			if (visitor is IfStatementSyntax { Else: { } })
				return null;

			visitor = visitor.Parent;
		}

		return new TryGetComponentContext(targetIdentifier.Value.Text, isVariableDeclaration, ifStatement, binaryExpression.Kind());
	}

	private static bool IsCompatibleGetComponent(InvocationExpressionSyntax invocation, SemanticModel model)
	{
		// We are looking for the exact GetComponent method, not other derivatives, so we do not want to use KnownMethods.IsGetComponentName(nameSyntax)
		if (invocation.GetMethodNameSyntax() is not { Identifier.Text: nameof(UnityEngine.Component.GetComponent) })
			return false;

		var symbol = model.GetSymbolInfo(invocation);
		if (symbol.Symbol is not IMethodSymbol method)
			return false;

		// We want Component.GetComponent or GameObject.GetComponent (given we already checked the exact name, we can use this one)
		if (!KnownMethods.IsGetComponent(method))
			return false;

		// We don't want arguments
		if (invocation.ArgumentList.Arguments.Count != 0)
			return false;

		// We want a type argument
		return method.TypeArguments.Length == 1;
	}

	private static bool TryGetConditionIdentifier(SyntaxNode ifNode, [NotNullWhen(true)] out SyntaxToken? conditionIdentifier, [NotNullWhen(true)] out BinaryExpressionSyntax? foundBinaryExpression, [NotNullWhen(true)] out IfStatementSyntax? foundIfStatement)
	{
		foundBinaryExpression = null;
		foundIfStatement = null;
		conditionIdentifier = null;

		if (ifNode is not IfStatementSyntax { Condition: BinaryExpressionSyntax binaryExpression } ifStatement)
			return false;

		foundBinaryExpression = binaryExpression;
		foundIfStatement = ifStatement;

		// We want an Equals/NotEquals condition
		if (!binaryExpression.IsKind(SyntaxKind.EqualsExpression) &&
		    !binaryExpression.IsKind(SyntaxKind.NotEqualsExpression))
			return false;

		// We want IdentifierNameSyntax and null as operands
		conditionIdentifier = binaryExpression.Left switch
		{
			IdentifierNameSyntax leftIdentifierName when binaryExpression.Right is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } => leftIdentifierName.Identifier,
			LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } when binaryExpression.Right is IdentifierNameSyntax rightIdentifierName => rightIdentifierName.Identifier,
			_ => null
		};

		return conditionIdentifier.HasValue;
	}

	private static bool TryGetTargetdentifier(SemanticModel model, SyntaxNode invocationParent, [NotNullWhen(true)] out SyntaxToken? targetIdentifier, out bool isVariableDeclaration)
	{
		targetIdentifier = null;
		isVariableDeclaration = false;

		switch (invocationParent)
		{
			case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variableDeclarator }:
				isVariableDeclaration = true;
				targetIdentifier = variableDeclarator.Identifier;
				break;

			case AssignmentExpressionSyntax { Left: IdentifierNameSyntax identifierName }:
			{
				// With an assignment, we want to work only with a field or local variable ('out' constraint)
				var symbol = model.GetSymbolInfo(identifierName);
				if (symbol.Symbol is not (ILocalSymbol or IFieldSymbol))
					return false;

				targetIdentifier = identifierName.Identifier;
				break;
			}
		}

		return targetIdentifier.HasValue;
	}

	private static bool TryGetNextTopNode(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? nextNode)
	{
		nextNode = null;
		var topNode = node;
		while (topNode.Parent != null && topNode.Parent is not BlockSyntax)
			topNode = topNode.Parent;

		if (topNode.Parent is not BlockSyntax block)
			return false;

		var siblingsAndSelf = block.ChildNodes().ToImmutableArray();
		var invocationIndex = siblingsAndSelf.IndexOf(topNode);
		if (invocationIndex == -1)
			return false;

		var ifIndex = invocationIndex + 1;
		if (ifIndex >= siblingsAndSelf.Length)
			return false;

		nextNode = siblingsAndSelf[ifIndex];
		return true;
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class TryGetComponentCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TryGetComponentAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var invocation = await context.GetFixableNodeAsync<InvocationExpressionSyntax>();
		if (invocation == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.TryGetComponentCodeFixTitle,
				ct => ReplaceWithTryGetComponentAsync(context.Document, invocation, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Document> ReplaceWithTryGetComponentAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
	{
		var model = await document.GetSemanticModelAsync(cancellationToken);
		if (model == null)
			return document;

		var context = TryGetComponentContext.GetContext(invocation, model);
		if (context == null)
			return document;

		SyntaxNode assignNode = invocation;
		while (assignNode.Parent != null && assignNode.Parent is not BlockSyntax)
			assignNode = assignNode.Parent;

		InvocationExpressionSyntax? newInvocation;
		var identifier = Identifier(nameof(UnityEngine.Component.TryGetComponent));

		// Direct method invocation or through a member
		switch (invocation.Expression)
		{
			case GenericNameSyntax directNameSyntax:
			{
				var newNameSyntax = directNameSyntax.WithIdentifier(identifier);
				newInvocation = invocation.WithExpression(newNameSyntax);
				break;
			}
			case MemberAccessExpressionSyntax { Name: GenericNameSyntax indirectNameSyntax } memberAccessExpression:
			{
				var newNameSyntax = indirectNameSyntax.WithIdentifier(identifier);
				var newMemberAccessExpression = memberAccessExpression.WithName(newNameSyntax);
				newInvocation = invocation.WithExpression(newMemberAccessExpression);
				break;
			}
			default:
				return document;
		}

		var targetIdentifier = context.TargetIdentifier;
		var newArgument = context.IsVariableDeclaration switch
		{
			// Creating var argument
			true => Argument(
				DeclarationExpression(
					IdentifierName(
						Identifier(TriviaList(),
							SyntaxKind.VarKeyword,
							"var",
							"var",
							TriviaList())),
					SingleVariableDesignation(
						Identifier(targetIdentifier)))),
			// Reusing the target identifier
			false => Argument(IdentifierName(targetIdentifier))
		};

		// Add the 'out' component argument 
		newInvocation = newInvocation
			.WithArgumentList(
				ArgumentList(
					SingletonSeparatedList(
						newArgument
							.WithRefOrOutKeyword(
								Token(SyntaxKind.OutKeyword)))));

		// We need to reverse the invocation value, if the original test is == null 
		ExpressionSyntax newCondition = context.ConditionKind switch
		{
			SyntaxKind.EqualsExpression => PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, newInvocation),
			_ => newInvocation
		};

		// Reuse inline ifExpressions of the original assignment
		var inlineIfAssignNode = assignNode;
		while (inlineIfAssignNode is IfStatementSyntax inlineIfStatement)
		{
			newCondition = BinaryExpression(SyntaxKind.LogicalAndExpression, inlineIfStatement.Condition, newCondition);
			inlineIfAssignNode = inlineIfStatement.Statement;
		}

		var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken);
		documentEditor.RemoveNode(assignNode, SyntaxRemoveOptions.KeepNoTrivia);

		var ifStatement = context.IfStatement;
		var newIfStatement = ifStatement
			.WithCondition(newCondition)
			.WithLeadingTrivia(assignNode.MergeLeadingTriviaWith(ifStatement));

		documentEditor.ReplaceNode(ifStatement, newIfStatement);

		return documentEditor.GetChangedDocument();
	}
}
