/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
			context.RegisterSyntaxNodeAction(AnalyzeBlock, SyntaxKind.Block);
			// TODO: context.RegisterSyntaxNodeAction
			// example: context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
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
			bool previous = false;
			
			foreach (var statement in block.Statements)
			{
				if (!(statement is ExpressionStatementSyntax))
					return;
				
				var expression = ((ExpressionStatementSyntax)statement).Expression;

				if (!(expression is AssignmentExpressionSyntax))
					return;

				if (!(expression is AssignmentExpressionSyntax))
					return;

				var assignmentExpression = (AssignmentExpressionSyntax)expression;

				if (!(assignmentExpression.Left is MemberAccessExpressionSyntax))
					return;

				var left = (MemberAccessExpressionSyntax)(assignmentExpression.Left);
				var property = left.Name.ToString();

				if (property != "position" && property != "rotation")
					return;

				var leftSymbol = context.SemanticModel.GetSymbolInfo(left);

				if (leftSymbol.Symbol == null)
					return;

				if (!(leftSymbol.Symbol is IPropertySymbol))
					return;

				var leftExpressionTypeInfo = context.SemanticModel.GetTypeInfo(left.Expression);

				if (leftExpressionTypeInfo.Type == null)
					return;

				if (!leftExpressionTypeInfo.Type.Extends(typeof(UnityEngine.Component)))
					return;

				if (property == "position")
					Debug.WriteLine("POSITION");
					Debug.WriteLine(rotation.ToString());
					if (rotation == true)
						Debug.WriteLine("POSITION & Rotation 1");
						context.ReportDiagnostic(Diagnostic.Create(Rule, expression.GetLocation()));
					position = true;

				if (property == "rotation")
					Debug.WriteLine("POSITION");
					Debug.WriteLine(rotation.ToString());
					Debug.WriteLine(position.ToString());
					Debug.WriteLine(left.ToString());
					if (position)
						Debug.WriteLine("POSITION AND ROTATION 2");
						context.ReportDiagnostic(Diagnostic.Create(Rule, expression.GetLocation()));
					rotation = true;
				//testing
			}

			return;
		}



		private static void AnalyzeExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
		{

			if (!(expression is AssignmentExpressionSyntax))
				return;

			var assignmentExpression = (AssignmentExpressionSyntax)expression;

			if (!(assignmentExpression.Left is MemberAccessExpressionSyntax))
				return;

			var left = (MemberAccessExpressionSyntax)(assignmentExpression.Left);

			if (left.Name.ToString() != "position" && left.Name.ToString() != "rotation")
				return;
		
			var leftSymbol = context.SemanticModel.GetSymbolInfo(left);
			
			if (leftSymbol.Symbol == null)
				return;

			if (!(leftSymbol.Symbol is IPropertySymbol))
				return;

			var leftExpressionTypeInfo = context.SemanticModel.GetTypeInfo(left.Expression);

			if (leftExpressionTypeInfo.Type == null)
				return;

			if (!leftExpressionTypeInfo.Type.Extends(typeof(UnityEngine.Component)))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, expression.GetLocation(), assignmentExpression.Right));

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
