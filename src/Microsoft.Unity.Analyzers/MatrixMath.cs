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
using Microsoft.Unity.Analyzers.Resources;
using UnityEngine;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class MatrixMathAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new(
			id: "UNT0024",
			title: Strings.MatrixMathDiagnosticTitle,
			messageFormat: Strings.MatrixMathDiagnosticMessageFormat,
			category: DiagnosticCategory.Performance,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.MatrixMathDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		internal Type[] SupportedTypes =
		{
			typeof(Vector2),
			typeof(Vector3),
			typeof(Vector4)
		};

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			
			context.RegisterSyntaxNodeAction(AnalyzeMultiplyExpression, SyntaxKind.MultiplyExpression);
		}

		private void AnalyzeMultiplyExpression(SyntaxNodeAnalysisContext context)
		{
			var expr = context.Node;

			foreach (var type in SupportedTypes)
			{
				// Only keep the top-level expression, so make sure we don't have a reportable parent
				if (!IsReportableExpression(context, expr, type) || IsReportableExpression(context, expr.Parent, type))
					continue;

				context.ReportDiagnostic(Diagnostic.Create(Rule, expr.GetLocation()));
			}
		}

		private bool IsReportableExpression(SyntaxNodeAnalysisContext context, SyntaxNode node, Type vectorType)
		{
			if (!IsCandidateExpression(context, node, vectorType))
				return false;

			return NeedsOrdering(context, (ExpressionSyntax)node);
		}

		private static bool NeedsOrdering(SyntaxNodeAnalysisContext context, ExpressionSyntax node)
		{
			var operands = GetOperands(context.SemanticModel, node)
				.ToArray();

			if (operands.Length < 3)
				return false;

			int firstMatrixIndex = -1;
			int lastPrimitiveIndex = -1;
			int primitiveCount = 0;
			for (int i = 0; i < operands.Length; i++)
			{
				var (_, typeInfo) = operands[i];
				if (IsFloatType(typeInfo))
				{
					lastPrimitiveIndex = i;
					primitiveCount++;
				}
				else
				{
					if (firstMatrixIndex == -1)
						firstMatrixIndex = i;
				}
			}

			return firstMatrixIndex != -1
			       && lastPrimitiveIndex != -1
			       && firstMatrixIndex < lastPrimitiveIndex
			       && primitiveCount >= 2;
		}

		internal static bool IsFloatType(TypeInfo typeInfo)
		{
			return typeInfo.ConvertedType != null && typeInfo.ConvertedType.Matches(typeof(float));
		}

		internal static IEnumerable<(ExpressionSyntax, TypeInfo)> GetOperands(SemanticModel model, ExpressionSyntax node)
		{
			if (node is BinaryExpressionSyntax be)
			{
				foreach (var o in GetOperands(model, be.Left))
					yield return o;

				foreach (var o in GetOperands(model, be.Right))
					yield return o;
			}
			else
			{
				yield return new(node, model.GetTypeInfo(node));
			}
		}

		private static bool IsCandidateExpression(SyntaxNodeAnalysisContext context, SyntaxNode node, Type vectorType)
		{
			if (node is not BinaryExpressionSyntax be)
				return false;

			if (!node.IsKind(SyntaxKind.MultiplyExpression))
				return false;

			return IsSupportedExpression(context, be.Left, vectorType) && IsSupportedExpression(context, be.Right, vectorType);
		}

		private static bool IsSupportedExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expression, Type vectorType)
		{
			if (IsCandidateExpression(context, expression, vectorType))
				return true;

			var model = context.SemanticModel;
			var typeInfo = model.GetTypeInfo(expression);

			// We want to check the converted type here, given all is float-based.
			if (IsFloatType(typeInfo))
				return true;

			// Else we want a matrix-like item
			var type = typeInfo.Type;
			if (type != null && type.Matches(vectorType))
				return true;

			return false;
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class MatrixMathCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MatrixMathAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var node = await context.GetFixableNodeAsync<BinaryExpressionSyntax>();
			if (node == null)
				return;

			context.RegisterCodeFix(
				CodeAction.Create(
					Strings.MatrixMathCodeFixTitle,
					ct => FixOperandOrderAsync(context.Document, node, ct),
					node.ToFullString()),
				context.Diagnostics);
		}

		private static async Task<Document> FixOperandOrderAsync(Document document, BinaryExpressionSyntax node, CancellationToken ct)
		{
			var root = await document
				.GetSyntaxRootAsync(ct)
				.ConfigureAwait(false);

			if (root == null)
				return document;

			var model = await document
				.GetSemanticModelAsync(ct)
				.ConfigureAwait(false);

			if (model == null)
				return document;

			var operands = MatrixMathAnalyzer.GetOperands(model, node)
				.OrderBy(GetTypePriority)
				.ThenBy(GetSyntaxPriority)
				.Select(o => o.Item1)
				.ToList();

			var newNode = BuildMultiplyExpression(operands)
				.WithTriviaFrom(node);

			var newRoot = root.ReplaceNode(node, newNode);
			if (newRoot == null)
				return document;

			return document.WithSyntaxRoot(newRoot);
		}

		private static int GetTypePriority((ExpressionSyntax, TypeInfo) item)
		{
			var (_, typeInfo) = item;
			if (MatrixMathAnalyzer.IsFloatType(typeInfo))
				return 0;

			return 1;
		}

		private static string GetSyntaxPriority((ExpressionSyntax, TypeInfo) item)
		{
			var (syntax, _) = item;
			return syntax.ToString();
		}

		private static ExpressionSyntax BuildMultiplyExpression(IList<ExpressionSyntax> operands)
		{
			var first = operands.First();
			operands.RemoveAt(0);

			if (operands.Count == 1)
				return SyntaxFactory.BinaryExpression(
					SyntaxKind.MultiplyExpression,
					first.WithoutTrivia(),
					operands.Last().WithoutTrivia());

			return SyntaxFactory.BinaryExpression(
				SyntaxKind.MultiplyExpression,
				first.WithoutTrivia(),
				BuildMultiplyExpression(operands));
		}
	}
}
