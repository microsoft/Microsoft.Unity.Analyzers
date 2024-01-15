/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
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

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InputGetKeyAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0025";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.InputGetKeyDiagnosticTitle,
		messageFormat: Strings.InputGetKeyDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.InputGetKeyDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	private static readonly Lazy<Dictionary<string, KeyCode>> _lookup = new(BuildLookup);

	private static Dictionary<string, KeyCode> BuildLookup()
	{
		var enumType = typeof(KeyCode);
		var values = Enum.GetValues(enumType);
		var lookup = new Dictionary<string, KeyCode>();

		foreach (KeyCode item in values)
		{
			var fieldInfo = enumType.GetField(item.ToString());
			if (fieldInfo == null)
				continue;

			var attribute = fieldInfo.GetCustomAttributes<KeyTextAttribute>(false).FirstOrDefault();
			var key = attribute?.Text;
			if (key == null)
				continue;

			lookup.Add(key, item);
		}

		return lookup;
	}

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not InvocationExpressionSyntax invocation)
			return;

		if (!IsInvocationSupported(invocation))
			return;

		var symbol = context.SemanticModel.GetSymbolInfo(invocation);
		if (symbol.Symbol is not IMethodSymbol method)
			return;

		if (!IsMethodSupported(method))
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
	}

	private static bool IsInvocationSupported(InvocationExpressionSyntax invocation)
	{
		if (invocation.ArgumentList.Arguments.Count != 1)
			return false;

		var argument = invocation.ArgumentList.Arguments.First();
		return IsArgumentSupported(argument);
	}

	private static bool IsMethodSupported(IMethodSymbol method)
	{
		return method.Name switch
		{
			nameof(Input.GetKey) or nameof(Input.GetKeyDown) or nameof(Input.GetKeyUp) => method.ContainingType.Matches(typeof(Input)),
			_ => false,
		};
	}

	private static bool IsArgumentSupported(ArgumentSyntax argument)
	{
		if (argument.Expression is not LiteralExpressionSyntax les)
			return false;

		if (les.Kind() != SyntaxKind.StringLiteralExpression)
			return false;

		return TryParse(les, out _);
	}

	internal static bool TryParse(LiteralExpressionSyntax les, out KeyCode value)
	{
		var name = les
			.Token
			.ValueText
			.ToLower();

		return _lookup.Value.TryGetValue(name, out value);
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class InputGetKeyCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(InputGetKeyAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var invocation = await context.GetFixableNodeAsync<InvocationExpressionSyntax>();
		if (invocation == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.InputGetKeyCodeFixTitle,
				ct => UseKeyCodeMemberAsArgumentAsync(context.Document, invocation, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private async Task<Document> UseKeyCodeMemberAsArgumentAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken ct)
	{
		var root = await document.GetSyntaxRootAsync(ct)
			.ConfigureAwait(false);

		// We already know that we have one literal string argument
		var argument = invocation.ArgumentList.Arguments[0];
		var les = (LiteralExpressionSyntax)argument.Expression;

		var newInvocation = invocation
			.WithArgumentList(invocation
				.ArgumentList
				.ReplaceNode(argument, GetKeyCodeArgument(les)
					.WithTrailingTrivia(argument.GetTrailingTrivia())));

		var newRoot = root?.ReplaceNode(invocation, newInvocation);
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}

	private static ArgumentSyntax GetKeyCodeArgument(LiteralExpressionSyntax les)
	{
		if (!InputGetKeyAnalyzer.TryParse(les, out var result))
			throw new ArgumentException(nameof(les));

		return SyntaxFactory.Argument(
			SyntaxFactory.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				SyntaxFactory.IdentifierName(nameof(KeyCode)),
				SyntaxFactory.IdentifierName(result.ToString())));
	}
}
