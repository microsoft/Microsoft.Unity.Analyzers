/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

	private const int LongSymbolLength = 8;
	private const int MaxShortSymbolEditDistance = 1;
	private const int MaxLongSymbolEditDistance = 2;
	private const int MaxStackAllocEditDistanceBufferLength = 64;

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

		var knownSymbols = GetKnownSymbols(parseOptions);
		if (knownSymbols.Length == 0)
			return;

		var root = context.Tree.GetRoot(context.CancellationToken);
		var knownSymbolSet = new HashSet<string>(knownSymbols, StringComparer.Ordinal);
		var localSymbols = GetLocalSymbols(root);

		foreach (var condition in GetDirectiveConditions(root))
		{
			foreach (var identifier in condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				var symbol = identifier.Identifier.ValueText;
				if (string.IsNullOrEmpty(symbol))
					continue;

				if (knownSymbolSet.Contains(symbol) || localSymbols.Contains(symbol))
					continue;

				if (!TryFindClosestSymbol(symbol, knownSymbols, out var closestSymbol))
					continue;

				context.ReportDiagnostic(Diagnostic.Create(
					Rule,
					identifier.GetLocation(),
					null,
					ImmutableDictionary<string, string?>.Empty.Add(SuggestedSymbolPropertyName, closestSymbol),
					symbol,
					closestSymbol));
			}
		}
	}

	private static string[] GetKnownSymbols(CSharpParseOptions parseOptions)
	{
		return [.. parseOptions.PreprocessorSymbolNames
			.Where(symbol => !string.IsNullOrWhiteSpace(symbol))
			.Distinct(StringComparer.Ordinal)
			.OrderBy(symbol => symbol, StringComparer.Ordinal)];
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

	private static bool TryFindClosestSymbol(string symbol, string[] candidates, [NotNullWhen(true)] out string? closestSymbol)
	{
		closestSymbol = null;
		if (symbol.Length < 3)
			return false;

		var maxDistance = GetMaxEditDistance(symbol);
		var bestDistance = maxDistance + 1;
		var bestLengthDifference = int.MaxValue;

		foreach (var candidate in candidates)
		{
			var lengthDifference = Math.Abs(symbol.Length - candidate.Length);
			if (lengthDifference > maxDistance)
				continue;

			var distance = GetEditDistance(symbol, candidate, maxDistance);
			if (distance > maxDistance)
				continue;

			if (distance > bestDistance)
				continue;

			if (distance == bestDistance && lengthDifference >= bestLengthDifference)
				continue;

			closestSymbol = candidate;
			bestDistance = distance;
			bestLengthDifference = lengthDifference;
		}

		return closestSymbol != null;
	}

	private static int GetMaxEditDistance(string symbol)
	{
		return symbol.Length >= LongSymbolLength ? MaxLongSymbolEditDistance : MaxShortSymbolEditDistance;
	}

	private static int GetEditDistance(string source, string target, int maxDistance)
	{
		if (source.Length == 0)
			return target.Length;

		if (target.Length == 0)
			return source.Length;

		if (Math.Abs(source.Length - target.Length) > maxDistance)
			return maxDistance + 1;

		var bufferLength = target.Length + 1;
		var previous = bufferLength <= MaxStackAllocEditDistanceBufferLength ? stackalloc int[bufferLength] : new int[bufferLength];
		var current = bufferLength <= MaxStackAllocEditDistanceBufferLength ? stackalloc int[bufferLength] : new int[bufferLength];

		for (var i = 0; i <= target.Length; i++)
			previous[i] = i;

		for (var sourceIndex = 1; sourceIndex <= source.Length; sourceIndex++)
		{
			current[0] = sourceIndex;
			var rowMinimum = current[0];

			for (var targetIndex = 1; targetIndex <= target.Length; targetIndex++)
			{
				var substitutionCost = source[sourceIndex - 1] == target[targetIndex - 1] ? 0 : 1;
				var deletion = previous[targetIndex] + 1;
				var insertion = current[targetIndex - 1] + 1;
				var substitution = previous[targetIndex - 1] + substitutionCost;
				var distance = Math.Min(Math.Min(deletion, insertion), substitution);

				current[targetIndex] = distance;
				rowMinimum = Math.Min(rowMinimum, distance);
			}

			if (rowMinimum > maxDistance)
				return maxDistance + 1;

			var nextPrevious = previous;
			previous = current;
			current = nextPrevious;
		}

		return previous[target.Length];
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
