/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
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

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class SetPositionAndRotationAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0022",
			title: Strings.SetPositionAndRotationDiagnosticTitle,
			messageFormat: Strings.SetPositionAndRotationDiagnosticMessageFormat,
			category: DiagnosticCategory.Performance,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.SetPositionAndRotationDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
		}

		private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
		{
			if (!(context.Node is AssignmentExpressionSyntax assignmentExpression))
				return;

			if (!IsSetPositionOrRotation(assignmentExpression, context.SemanticModel))
				return;


			if (context.Node.FirstAncestorOrSelf<BlockSyntax>() == null)
				return;

			var block = context.Node.FirstAncestorOrSelf<BlockSyntax>();

			if (context.Node.FirstAncestorOrSelf<ExpressionStatementSyntax>() == null)
				return;

			var expression = context.Node.FirstAncestorOrSelf<ExpressionStatementSyntax>();

			var siblingsAndSelf = block.ChildNodes().ToImmutableArray();

			if (siblingsAndSelf.LastIndexOf(expression) == -1)
				return;

			var currentIndex = siblingsAndSelf.LastIndexOf(expression);

			var nextIndex = currentIndex + 1;

			if (nextIndex == siblingsAndSelf.Length)
				return;

			var statement = siblingsAndSelf[nextIndex];

			if (!(statement is ExpressionStatementSyntax expressionStatement))
				return;

			if (!(expressionStatement.Expression is AssignmentExpressionSyntax nextAssignmentExpression))
				return;

			if (!IsSetPositionOrRotation(nextAssignmentExpression, context.SemanticModel))
				return;

			var property = GetProperty(assignmentExpression);

			var nextProperty = GetProperty(nextAssignmentExpression);

			if (property == nextProperty)
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
		}

		private static string GetProperty(AssignmentExpressionSyntax assignmentExpression)
		{
			if (!(assignmentExpression.Left is MemberAccessExpressionSyntax left))
				return string.Empty;

			return left.Name.ToString();
		}

		private static bool IsSetPositionOrRotation(AssignmentExpressionSyntax assignmentExpression, SemanticModel model)
		{

			if (!(assignmentExpression.Left is MemberAccessExpressionSyntax left))
				return false;

			var property = GetProperty(assignmentExpression);

			if (property != "position" && property != "rotation")
				return false;

			var leftSymbol = model.GetSymbolInfo(left);

			if (leftSymbol.Symbol == null)
				return false;

			if (!(leftSymbol.Symbol is IPropertySymbol))
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
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (!(root?.FindNode(context.Span) is AssignmentExpressionSyntax expression))
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.SetPositionAndRotationCodeFixTitle,
					ct => ReplaceWithInvocationAsync(context.Document, expression, "transform", "SetPositionAndRotation", ct),
					expression.ToFullString()),
				context.Diagnostics);
		}
		private static async Task<Document> ReplaceWithInvocationAsync(Document document, AssignmentExpressionSyntax assignmentExpression, string identifierName, string genericMethodName, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

			if (assignmentExpression.FirstAncestorOrSelf<BlockSyntax>() == null)
				return document;

			var block = assignmentExpression.FirstAncestorOrSelf<BlockSyntax>();

			if (assignmentExpression.FirstAncestorOrSelf<ExpressionStatementSyntax>() == null)
				return document;

			var expression = assignmentExpression.FirstAncestorOrSelf<ExpressionStatementSyntax>();

			var siblingsAndSelf = block.ChildNodes().ToImmutableArray();

			if (siblingsAndSelf.LastIndexOf(expression) == -1)
				return document;

			var currentIndex = siblingsAndSelf.LastIndexOf(expression);

			var nextIndex = currentIndex + 1;

			if (nextIndex == siblingsAndSelf.Length)
				return document;

			var statement = siblingsAndSelf[nextIndex];

			if (!(statement is ExpressionStatementSyntax expressionStatement))
				return document;

			if (!(expressionStatement.Expression is AssignmentExpressionSyntax nextAssignmentExpression))
				return null;

			if (assignmentExpression.Right == null || nextAssignmentExpression.Right == null)
				return document;

			var argList = ArgumentList();

			var property = SetPositionAndRotationAnalyzer.GetProperty(assignmentExpression);

			var arguments = new[] {Argument(assignmentExpression.Right), Argument(nextAssignmentExpression.Right)};
			if (property != "position")
				Array.Reverse(arguments);

			var argList = ArgumentList()
				.AddArguments(arguments);

			var invocation = InvocationExpression(
					MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName(identifierName),
						IdentifierName(methodName)))
				.WithArgumentList(argList);

			var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken);
			documentEditor.RemoveNode(statement);
			documentEditor.ReplaceNode(assignmentExpression, invocation);

			return documentEditor.GetChangedDocument();

		}
	}
}
