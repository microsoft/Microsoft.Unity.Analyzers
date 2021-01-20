/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class SetPositionAndRotationAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0021",
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


			if (!IsSetPositionOrRotation(context))
				return;

			var assignmentExpression = (AssignmentExpressionSyntax)context.Node;

			var block = assignmentExpression.FirstAncestorOrSelf<BlockSyntax>();

			var children = block.ChildNodes();

			//foreach (var statement in block.ChildThatContainsPosition(0) ChildNodes())
			//{
			//	Debug.WriteLine("statements");
			//	Debug.WriteLine(statement);
			//}

			////assignmentExpression.;
			//var currentIndex = block.Statements.IndexOf((StatementSyntax)context.Node);
			//var nextStatement = block.Statements.Where(s => block.ChildThatContainsPosition(0));
			//Debug.WriteLine(loc);
		}

		private static void AnalyzeBlock(SyntaxNodeAnalysisContext context)
		{
			if (!(context.Node is BlockSyntax))
				return;

			var block = (BlockSyntax)context.Node;


			var statements = block.Statements;

			

			if (statements.Count < 2)
				return;

			bool position = false;
			bool rotation = false;
			//var positionAssignment;
			//var rotationAssignment;

			//foreach (var statement in block.Statements.Where(d => IsSetPositionOrRotation(context, d) == true))
			//{
			//	Debug.WriteLine("YAY");
			//	Debug.WriteLine(statement);
			//	Debug.WriteLine(block.Statements.IndexOf(statement));
			//}

			foreach (var statement in block.Statements)
			{
				
				// if (!CheckPositionOrRotation = boolean)
				//rotation = false
				// position == false
				// continue
				// String getName = "position" or "rotation"
				// if position: position = true
				// if rotation: rotation = true
				// if (position && rotation): report

				//if (!IsSetPositionOrRotation(context, statement))
				//{
				//	rotation = false;
				//	position = false;
				//	continue;
				//}
				//var property = (MemberAccessExpressionSyntax)((ExpressionStatementSyntax)statement).Expression);

				//if (property == "rotation")
				//	rotation = true;

				//if (property == "position")
				//	position = true;

				if (rotation && position)
					context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));

			}

			return;
		}



		private static bool IsSetPositionOrRotation(SyntaxNodeAnalysisContext context)
		{
			var assignmentExpression = (AssignmentExpressionSyntax)context.Node;

			if (!(assignmentExpression.Left is MemberAccessExpressionSyntax))
				return false;

			var left = (MemberAccessExpressionSyntax)(assignmentExpression.Left);

			var property = left.Name.ToString();

			if (property != "position" && property != "rotation")
				return false;

			var leftSymbol = context.SemanticModel.GetSymbolInfo(left);

			if (leftSymbol.Symbol == null)
				return false;

			if (!(leftSymbol.Symbol is IPropertySymbol))
				return false;

			var leftExpressionTypeInfo = context.SemanticModel.GetTypeInfo(left.Expression);

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
