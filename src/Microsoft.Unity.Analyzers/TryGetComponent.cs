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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TryGetComponentAnalyzer : BaseGetComponentAnalyzer
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
}

internal class TryGetComponentContext(string targetIdentifier, IfStatementSyntax ifStatement, SyntaxKind conditionKind)
{
	public string TargetIdentifier { get; } = targetIdentifier;
	public IfStatementSyntax IfStatement { get; } = ifStatement;
	public SyntaxKind ConditionKind { get; } = conditionKind;

	public static TryGetComponentContext? GetContext(InvocationExpressionSyntax invocation, SemanticModel model)
	{
		// We want the generic GetComponent, no arguments
		if (!BaseGetComponentAnalyzer.IsGenericGetComponent(invocation, model, out _))
			return null;

		// We want a variable declaration with invocation as initializer
		var invocationParent = invocation.Parent;
		if (invocationParent == null)
			return null;

		if (!BaseGetComponentAnalyzer.TryGetTargetdentifier(invocationParent, out var targetIdentifier))
			return null;

		// We want the next line to be an if statement
		if (!BaseGetComponentAnalyzer.TryGetNextTopNode(invocationParent, out var ifNode))
			return null;

		// We want a binary expression
		if (!BaseGetComponentAnalyzer.TryGetConditionIdentifier(ifNode, out var conditionIdentifier, out var binaryExpression, out var ifStatement))
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

			if (visitor is IfStatementSyntax { Else: not null })
				return null;

			visitor = visitor.Parent;
		}

		return new TryGetComponentContext(targetIdentifier.Value.Text, ifStatement, binaryExpression.Kind());
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

		// Creating var argument
		var newArgument = Argument(
			DeclarationExpression(
				IdentifierName(
					Identifier(TriviaList(),
						SyntaxKind.VarKeyword,
						"var",
						"var",
						TriviaList())),
				SingleVariableDesignation(
					Identifier(targetIdentifier))));

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
