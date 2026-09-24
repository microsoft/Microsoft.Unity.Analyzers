/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConditionalCompilationSymbolTypoAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0043";
	internal const string SuggestedSymbolPropertyName = "SuggestedSymbol";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.ConditionalCompilationSymbolTypoDiagnosticTitle,
		messageFormat: Strings.ConditionalCompilationSymbolTypoDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.ConditionalCompilationSymbolTypoDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	// UNITY_X, UNITY_X_Y, UNITY_X_Y_Z, and UNITY_X_Y_OR_NEWER.
	private static readonly Regex UnityVersionSymbolRegex = new(@"\AUNITY_[0-9]+(?:_[0-9]+(?:_[0-9]+|_OR_NEWER)?)?\z", RegexOptions.Compiled);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
	}

	private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
	{
		if (context.Tree.Options is not CSharpParseOptions parseOptions)
			return;

		var definedSymbols = GetDefinedSymbols(parseOptions);
		if (definedSymbols.Length == 0)
			return;

		var root = context.Tree.GetRoot(context.CancellationToken);
		var definedSymbolSet = new HashSet<string>(definedSymbols, StringComparer.Ordinal);
		var localSymbols = GetLocalSymbols(root);

		foreach (var condition in GetDirectiveConditions(root))
		{
			foreach (var identifier in condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				var symbol = identifier.Identifier.ValueText;
				if (string.IsNullOrEmpty(symbol))
					continue;

				if (definedSymbolSet.Contains(symbol) || localSymbols.Contains(symbol) || UnityVersionSymbolRegex.IsMatch(symbol))
					continue;

				if (!TryFindSuggestedSymbol(symbol, definedSymbols, out var suggestedSymbol))
					continue;

				context.ReportDiagnostic(Diagnostic.Create(
					Rule,
					identifier.GetLocation(),
					null,
					ImmutableDictionary<string, string?>.Empty.Add(SuggestedSymbolPropertyName, suggestedSymbol),
					symbol,
					suggestedSymbol));
			}
		}
	}

	private static string[] GetDefinedSymbols(CSharpParseOptions parseOptions)
	{
		return [.. parseOptions.PreprocessorSymbolNames
			.Where(symbol => !string.IsNullOrWhiteSpace(symbol))
			.Distinct(StringComparer.Ordinal)];
	}

	private static HashSet<string> GetLocalSymbols(SyntaxNode root)
	{
		var symbols = new HashSet<string>(StringComparer.Ordinal);
		foreach (var trivia in root.DescendantTrivia())
		{
			if (trivia.GetStructure() is not DefineDirectiveTriviaSyntax defineDirective)
				continue;

			var symbol = defineDirective.Name.ValueText;
			if (!string.IsNullOrEmpty(symbol))
				symbols.Add(symbol);
		}

		return symbols;
	}

	private static IEnumerable<ExpressionSyntax> GetDirectiveConditions(SyntaxNode root)
	{
		foreach (var trivia in root.DescendantTrivia())
		{
			switch (trivia.GetStructure())
			{
				case IfDirectiveTriviaSyntax ifDirective:
					yield return ifDirective.Condition;
					break;
				case ElifDirectiveTriviaSyntax elifDirective:
					yield return elifDirective.Condition;
					break;
			}
		}
	}

	private static bool TryFindSuggestedSymbol(string symbol, string[] candidates, [NotNullWhen(true)] out string? suggestedSymbol)
	{
		suggestedSymbol = null;
		if (symbol.Length < 3)
			return false;

		foreach (var candidate in candidates)
		{
			if (!IsSingleEdit(symbol.AsSpan(), candidate.AsSpan()) ||
				!HaveSameNumericComponents(symbol.AsSpan(), candidate.AsSpan()))
				continue;

			if (suggestedSymbol != null)
			{
				suggestedSymbol = null;
				return false;
			}

			suggestedSymbol = candidate;
		}

		return suggestedSymbol != null;
	}

	private static bool IsSingleEdit(ReadOnlySpan<char> source, ReadOnlySpan<char> target)
	{
		if (Math.Abs(source.Length - target.Length) > 1)
			return false;

		var index = 0;
		while (index < source.Length && index < target.Length && source[index] == target[index])
			index++;

		if (source.Length != target.Length)
		{
			return source.Length > target.Length
				? source.Slice(index + 1).SequenceEqual(target.Slice(index))
				: source.Slice(index).SequenceEqual(target.Slice(index + 1));
		}

		if (index == source.Length)
			return false;

		if (source.Slice(index + 1).SequenceEqual(target.Slice(index + 1)))
			return true;

		return index + 1 < source.Length &&
			source[index] == target[index + 1] &&
			source[index + 1] == target[index] &&
			source.Slice(index + 2).SequenceEqual(target.Slice(index + 2));
	}

	private static bool HaveSameNumericComponents(ReadOnlySpan<char> source, ReadOnlySpan<char> target)
	{
		var sourceIndex = 0;
		var targetIndex = 0;
		while (sourceIndex < source.Length || targetIndex < target.Length)
		{
			var sourceComponent = GetNextNumericComponent(source, ref sourceIndex);
			var targetComponent = GetNextNumericComponent(target, ref targetIndex);
			if (!sourceComponent.SequenceEqual(targetComponent))
				return false;
		}

		return true;
	}

	private static ReadOnlySpan<char> GetNextNumericComponent(ReadOnlySpan<char> symbol, ref int index)
	{
		while (index < symbol.Length && !char.IsDigit(symbol[index]))
			index++;

		var start = index;
		while (index < symbol.Length && char.IsDigit(symbol[index]))
			index++;

		return symbol.Slice(start, index - start);
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class ConditionalCompilationSymbolTypoCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ConditionalCompilationSymbolTypoAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics.FirstOrDefault();
		if (diagnostic == null)
			return Task.CompletedTask;

		if (!diagnostic.Properties.TryGetValue(ConditionalCompilationSymbolTypoAnalyzer.SuggestedSymbolPropertyName, out var suggestedSymbol) ||
			suggestedSymbol is not { Length: > 0 } replacement)
			return Task.CompletedTask;

		context.RegisterCodeFix(
			CodeAction.Create(
				string.Format(Strings.ConditionalCompilationSymbolTypoCodeFixTitle, replacement),
				ct => ReplaceSymbolAsync(context.Document, context.Span, replacement, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);

		return Task.CompletedTask;
	}

	private static async Task<Document> ReplaceSymbolAsync(Document document, TextSpan span, string symbol, CancellationToken cancellationToken)
	{
		var text = await document
			.GetTextAsync(cancellationToken)
			.ConfigureAwait(false);

		return document.WithText(text.WithChanges(new TextChange(span, symbol)));
	}
}
