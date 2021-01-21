/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

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
			if (!(context.Node is AssignmentExpressionSyntax))
				return;

			var assignmentExpression = (AssignmentExpressionSyntax)context.Node;

			if (!IsSetPositionOrRotation(assignmentExpression, context.SemanticModel))
				return;

			
			if (context.Node.FirstAncestorOrSelf<BlockSyntax>() == null)
				return;

			var block = context.Node.FirstAncestorOrSelf<BlockSyntax>();

			if (context.Node.FirstAncestorOrSelf<ExpressionStatementSyntax>() == null)
				return;
			
			var expression = context.Node.FirstAncestorOrSelf<ExpressionStatementSyntax>();

			var siblingsAndSelf = block.ChildNodes().ToImmutableArray();
			var currentIndex = siblingsAndSelf.LastIndexOf(expression);
			var nextIndex = currentIndex + 1;
			if (nextIndex == siblingsAndSelf.Length)
				return;

			var statement = siblingsAndSelf[nextIndex];


			if (!(statement is ExpressionStatementSyntax))
				return;

			var nextExpression = ((ExpressionStatementSyntax)statement).Expression;


			if (!(nextExpression is AssignmentExpressionSyntax))
				return;


			var nextAssignmentExpression = (AssignmentExpressionSyntax)nextExpression;

			if (!IsSetPositionOrRotation(nextAssignmentExpression, context.SemanticModel))
				return;

			Debug.WriteLine("here");

			var property = GetProperty(assignmentExpression);

			var nextProperty = GetProperty(nextAssignmentExpression);
			Debug.WriteLine(property);
			Debug.WriteLine(nextProperty);
			if (property == nextProperty)
				return;
			
			var assignmnet = assignmentExpression.Right;

			var nextAssignment = nextAssignmentExpression.Right;

			context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(),property, assignmnet,nextProperty,nextAssignment));
		}

		private static string GetProperty(AssignmentExpressionSyntax assignmentExpression)
		{
			var left = (MemberAccessExpressionSyntax)(assignmentExpression.Left);

			var property = left.Name.ToString();

			return property;
		}

		private static bool IsSetPositionOrRotation(AssignmentExpressionSyntax assignmentExpression, SemanticModel model)
		{

			if (!(assignmentExpression.Left is MemberAccessExpressionSyntax))
				return false;

			var left = (MemberAccessExpressionSyntax)(assignmentExpression.Left);

			var property = left.Name.ToString();

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

			if (!leftExpressionTypeInfo.Type.Extends(typeof(UnityEngine.Transform)))
				return false;

			return true;

		}
	}

		[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class SetPositionAndRotationCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SetPositionAndRotationAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			// var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			// var declaration = root.FindNode(context.Span) as MethodDeclarationSyntax;
			// if (declaration == null)
			//     return;

			// context.RegisterCodeFix(
			//     CodeAction.Create(
			//         Strings.SetPositionAndRotationCodeFixTitle,
			//         ct => {},
			//         declaration.ToFullString()),
			//     context.Diagnostics);
		}
	}
}
