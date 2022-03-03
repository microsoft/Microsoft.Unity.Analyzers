/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.Unity.Analyzers.Resources;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetPositionAndRotationAnalyzer : DiagnosticAnalyzer
{
	internal static readonly DiagnosticDescriptor Rule = new(
		id: "UNT0022",
		title: Strings.SetPositionAndRotationDiagnosticTitle,
		messageFormat: Strings.SetPositionAndRotationDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: Strings.SetPositionAndRotationDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	internal const string Position = "position";
	internal const string Rotation = "rotation";

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
	}

	private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not AssignmentExpressionSyntax assignmentExpression)
			return;

		if (!GetNextAssignmentExpression(context.SemanticModel, assignmentExpression, out var nextAssignmentExpression))
			return;

		// We know that both assignmentExpression.Left and nextAssignmentExpression.Left are MemberAccessExpressionSyntax
		// cf. GetNextAssignmentExpression calling IsSetPositionOrRotation
		if (((MemberAccessExpressionSyntax)assignmentExpression.Left).Expression.ToString() != ((MemberAccessExpressionSyntax)nextAssignmentExpression.Left).Expression.ToString())
			return;

		var property = GetProperty(assignmentExpression);
		var nextProperty = GetProperty(nextAssignmentExpression);
		if (property == nextProperty)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, assignmentExpression.GetLocation()));
	}

	internal static bool GetNextAssignmentExpression(SemanticModel model, AssignmentExpressionSyntax assignmentExpression, [NotNullWhen(true)] out AssignmentExpressionSyntax? assignmentExpressionSyntax)
	{
		assignmentExpressionSyntax = null;

		if (!IsSetPositionOrRotation(model, assignmentExpression))
			return false;

		if (assignmentExpression.FirstAncestorOrSelf<BlockSyntax>() == null)
			return false;

		if (assignmentExpression.FirstAncestorOrSelf<ExpressionStatementSyntax>() == null)
			return false;

		var block = assignmentExpression.FirstAncestorOrSelf<BlockSyntax>();
		var siblingsAndSelf = block.ChildNodes().ToImmutableArray();
		var expression = assignmentExpression.FirstAncestorOrSelf<ExpressionStatementSyntax>();

		var lastIndexOf = siblingsAndSelf.LastIndexOf(expression);
		if (lastIndexOf == -1)
			return false;

		var nextIndex = lastIndexOf + 1;
		if (nextIndex == siblingsAndSelf.Length)
			return false;

		var statement = siblingsAndSelf[nextIndex];
		if (statement is not ExpressionStatementSyntax {Expression: AssignmentExpressionSyntax nextAssignmentExpression})
			return false;

		if (!IsSetPositionOrRotation(model, nextAssignmentExpression))
			return false;

		assignmentExpressionSyntax = nextAssignmentExpression;
		return true;
	}

	internal static string GetProperty(AssignmentExpressionSyntax assignmentExpression)
	{
		if (assignmentExpression.Left is not MemberAccessExpressionSyntax left)
			return string.Empty;

		return left.Name.ToString();
	}

	private static bool IsSetPositionOrRotation(SemanticModel model, AssignmentExpressionSyntax assignmentExpression)
	{
		var property = GetProperty(assignmentExpression);
		if (property != Position && property != Rotation)
			return false;

		if (assignmentExpression.Left is not MemberAccessExpressionSyntax left)
			return false;

		var leftSymbol = model.GetSymbolInfo(left);
		if (leftSymbol.Symbol is not IPropertySymbol)
			return false;

		var leftExpressionTypeInfo = model.GetTypeInfo(left.Expression);
		if (leftExpressionTypeInfo.Type == null)
			return false;

		return leftExpressionTypeInfo.Type.Extends(typeof(UnityEngine.Transform));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class SetPositionAndRotationCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SetPositionAndRotationAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var expression = await context.GetFixableNodeAsync<AssignmentExpressionSyntax>();
		if (expression == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.SetPositionAndRotationCodeFixTitle,
				ct => ReplaceWithInvocationAsync(context.Document, expression, ct),
				expression.ToFullString()),
			context.Diagnostics);
	}

	private static async Task<Document> ReplaceWithInvocationAsync(Document document, AssignmentExpressionSyntax assignmentExpression, CancellationToken cancellationToken)
	{
		var model = await document.GetSemanticModelAsync(cancellationToken);
		if (model == null)
			return document;

		if (!SetPositionAndRotationAnalyzer.GetNextAssignmentExpression(model, assignmentExpression, out var nextAssignmentExpression))
			return document;

		var property = SetPositionAndRotationAnalyzer.GetProperty(assignmentExpression);
		var arguments = new[]
		{
			Argument(assignmentExpression.Right)
				.WithLeadingTrivia(assignmentExpression.OperatorToken.TrailingTrivia),
			Argument(nextAssignmentExpression.Right)
				.WithLeadingTrivia(nextAssignmentExpression.OperatorToken.TrailingTrivia)
		};

		if (property != SetPositionAndRotationAnalyzer.Position)
			Array.Reverse(arguments);

		var argList = ArgumentList()
			.AddArguments(arguments);

		var baseExpression = assignmentExpression
			.Left
			.FirstAncestorOrSelf<MemberAccessExpressionSyntax>()?.Expression;

		if (baseExpression == null)
			return document;

		var invocation = InvocationExpression(
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					baseExpression,
					IdentifierName("SetPositionAndRotation")))
			.WithArgumentList(argList)
			.WithLeadingTrivia(assignmentExpression.MergeLeadingTriviaWith(nextAssignmentExpression));

		var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken);
		documentEditor.RemoveNode(assignmentExpression.Parent, SyntaxRemoveOptions.KeepNoTrivia);
		documentEditor.ReplaceNode(nextAssignmentExpression, invocation);

		return documentEditor.GetChangedDocument();
	}
}
