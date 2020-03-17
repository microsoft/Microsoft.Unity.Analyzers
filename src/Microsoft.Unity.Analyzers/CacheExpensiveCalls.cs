/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
	public class CacheExpensiveCallsAnalyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0015",
			title: Strings.CacheExpensiveCallsAnalyzerDiagnosticTitle,
			messageFormat: Strings.CacheExpensiveCallsAnalyzerDiagnosticMessageFormat,
			category: DiagnosticCategory.Performance,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.CacheExpensiveCallsAnalyzerDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		private static readonly HashSet<string> MethodNames = new HashSet<string>(new[]
		{
			"GetComponent",
			"GetComponents",
			"GetComponentInChildren",
			"GetComponentsInChildren",
			"GetComponentInParent",
			"GetComponentsInParent",
		});

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
		}

		private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
		{
			if (!(context.Node is MethodDeclarationSyntax method))
				return;

			if (method.IsMessage(context, typeof(UnityEngine.MonoBehaviour), "Update") ||
				method.IsMessage(context, typeof(UnityEngine.MonoBehaviour), "FixedUpdate"))
			{
				AnalyzeInvocations(context, method);
			}
		}

		private static void AnalyzeInvocations(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method)
		{
			var invocations = method
				.DescendantNodes()
				.OfType<InvocationExpressionSyntax>();

			var memberAccessExpressions = method
				.DescendantNodes()
				.OfType<MemberAccessExpressionSyntax>();

			foreach (var invocation in invocations)
				AnalyzeInvocation(context, invocation);

			foreach (var maes in memberAccessExpressions)
				AnalyzeMemberAccess(context, maes);
		}

		private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, SyntaxNode node)
		{
			var symbol = context.SemanticModel.GetSymbolInfo(node);
			if (symbol.Symbol == null)
				return;

			switch (node)
			{
				case InvocationExpressionSyntax ies:
					if (IsExpensiveInvocation(symbol.Symbol, out var expensiveInvocation))
						context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), expensiveInvocation));
					break;
				case MemberAccessExpressionSyntax maes:
					if (IsExpensiveMemberAccess(symbol.Symbol, maes, out var expensiveMemberAccess))
						context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), expensiveMemberAccess));
					break;
			}
		}

		private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context, MemberAccessExpressionSyntax maes)
		{
			var symbol = context.SemanticModel.GetSymbolInfo(maes);
			if (symbol.Symbol == null)
				return;

			if (!IsExpensiveMemberAccess(symbol.Symbol, maes, out var expression))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, maes.GetLocation(), expression));
		}

		private static bool IsExpensiveInvocation(ISymbol symbol, out string methodName)
		{
			methodName = null;
			if (!(symbol is IMethodSymbol method))
				return false;

			var containingType = method.ContainingType;
			if (!containingType.Matches(typeof(UnityEngine.Component)) && !containingType.Matches(typeof(UnityEngine.GameObject)))
				return false;

			if (!MethodNames.Contains(method.Name))
				return false;

			methodName = method.Name;
			return true;
		}

		private static bool IsExpensiveMemberAccess(ISymbol symbol, MemberAccessExpressionSyntax maes, out string expression)
		{
			expression = null;
			var containingType = symbol.ContainingType;
			if (!containingType.Matches(typeof(UnityEngine.Camera)))
				return false;

			if (!(maes.Expression is IdentifierNameSyntax))
				return false;

			if (!(symbol.Name == "main"))
				return false;

			expression = containingType.Name + '.' + symbol.Name;
			return true;
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class CacheExpensiveCallsCodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CacheExpensiveCallsAnalyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		}
	}
}
