/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

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

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnityObjectNullHandlingAnalyzer : DiagnosticAnalyzer
{
	internal const string NullCoalescingRuleId = "UNT0007";
	internal const string NullPropagationRuleId = "UNT0008";
	internal const string CoalescingAssignmentRuleId = "UNT0023";
	internal const string IsPatternRuleId = "UNT0029";

	internal static readonly DiagnosticDescriptor NullCoalescingRule = new(
		id: NullCoalescingRuleId,
		title: Strings.UnityObjectNullCoalescingDiagnosticTitle,
		messageFormat: Strings.UnityObjectNullCoalescingDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(NullCoalescingRuleId),
		description: Strings.UnityObjectNullCoalescingDiagnosticDescription);

	internal static readonly DiagnosticDescriptor NullPropagationRule = new(
		id: NullPropagationRuleId,
		title: Strings.UnityObjectNullPropagationDiagnosticTitle,
		messageFormat: Strings.UnityObjectNullPropagationDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(NullPropagationRuleId),
		description: Strings.UnityObjectNullPropagationDiagnosticDescription);

	internal static readonly DiagnosticDescriptor CoalescingAssignmentRule = new(
		id: CoalescingAssignmentRuleId,
		title: Strings.UnityObjectCoalescingAssignmentDiagnosticTitle,
		messageFormat: Strings.UnityObjectCoalescingAssignmentDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(CoalescingAssignmentRuleId),
		description: Strings.UnityObjectCoalescingAssignmentDiagnosticDescription);

	internal static readonly DiagnosticDescriptor IsPatternRule = new(
		id: IsPatternRuleId,
		title: Strings.UnityObjectIsPatternDiagnosticTitle,
		messageFormat: Strings.UnityObjectIsPatternDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(IsPatternRuleId),
		description: Strings.UnityObjectIsPatternDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
		NullCoalescingRule,
		NullPropagationRule,
		CoalescingAssignmentRule,
		IsPatternRule
	);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeCoalesceExpression, SyntaxKind.CoalesceExpression);
		context.RegisterSyntaxNodeAction(AnalyzeConditionalAccessExpression, SyntaxKind.ConditionalAccessExpression);
		context.RegisterSyntaxNodeAction(AnalyzeCoalesceAssignmentExpression, SyntaxKind.CoalesceAssignmentExpression);
		context.RegisterSyntaxNodeAction(AnalyzeIsPatternExpression, SyntaxKind.IsPatternExpression);
	}

	private static void AnalyzeIsPatternExpression(SyntaxNodeAnalysisContext context)
	{
		var pattern = (IsPatternExpressionSyntax)context.Node;

		switch (pattern.Pattern)
		{
			// obj is null
			case ConstantPatternSyntax { Expression.RawKind: (int)SyntaxKind.NullLiteralExpression }:

			//obj is not null, we need roslyn 3.7.0 here for UnaryPatternSyntax type and SyntaxKind.NotPattern enum value
			case UnaryPatternSyntax { RawKind: (int)SyntaxKind.NotPattern, Pattern: ConstantPatternSyntax { Expression.RawKind: (int)SyntaxKind.NullLiteralExpression } }:
				AnalyzeExpression(pattern, pattern.Expression, context, IsPatternRule);
				break;
		}
	}

	private static void AnalyzeCoalesceAssignmentExpression(SyntaxNodeAnalysisContext context)
	{
		var assignment = (AssignmentExpressionSyntax)context.Node;
		AnalyzeExpression(assignment, assignment.Left, context, CoalescingAssignmentRule);
	}

	private static void AnalyzeCoalesceExpression(SyntaxNodeAnalysisContext context)
	{
		var binary = (BinaryExpressionSyntax)context.Node;
		AnalyzeExpression(binary, binary.Left, context, NullCoalescingRule);
	}

	private static void AnalyzeConditionalAccessExpression(SyntaxNodeAnalysisContext context)
	{
		var access = (ConditionalAccessExpressionSyntax)context.Node;
		AnalyzeExpression(access, access.Expression, context, NullPropagationRule);
	}

	private static void AnalyzeExpression(ExpressionSyntax originalExpression, ExpressionSyntax typedExpression, SyntaxNodeAnalysisContext context, DiagnosticDescriptor rule)
	{
		var type = context.SemanticModel.GetTypeInfo(typedExpression);
		if (type.Type == null)
			return;

		if (!type.Type.Extends(typeof(UnityEngine.Object)))
			return;

		context.ReportDiagnostic(Diagnostic.Create(rule, originalExpression.GetLocation(), originalExpression.ToFullString()));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class UnityObjectNullHandlingCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
		UnityObjectNullHandlingAnalyzer.NullCoalescingRule.Id,
		UnityObjectNullHandlingAnalyzer.NullPropagationRule.Id,
		UnityObjectNullHandlingAnalyzer.CoalescingAssignmentRule.Id,
		UnityObjectNullHandlingAnalyzer.IsPatternRule.Id
	);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var expression = await context.GetFixableNodeAsync<SyntaxNode>(c => c is BinaryExpressionSyntax or AssignmentExpressionSyntax or ConditionalAccessExpressionSyntax or IsPatternExpressionSyntax);
		if (expression == null)
			return;

		CodeAction action;
		switch (expression)
		{
			// Null coalescing
			case BinaryExpressionSyntax bes:
				if (HasSideEffect(bes.Left))
					return;

				action = CodeAction.Create(
					Strings.UnityObjectNullCoalescingCodeFixTitle,
					ct => ReplaceNullCoalescingAsync(context.Document, bes, ct),
					UnityObjectNullHandlingAnalyzer.NullCoalescingRuleId); // using DiagnosticId as equivalence key for BatchFixer
				break;

			// Null propagation
			case ConditionalAccessExpressionSyntax caes:
				if (HasSideEffect(caes.Expression))
					return;

				action = CodeAction.Create(
					Strings.UnityObjectNullPropagationCodeFixTitle,
					ct => ReplaceNullPropagationAsync(context.Document, caes, ct),
					UnityObjectNullHandlingAnalyzer.NullPropagationRuleId); // using DiagnosticId as equivalence key for BatchFixer
				break;

			// Coalescing assignment
			case AssignmentExpressionSyntax aes:
				if (HasSideEffect(aes.Left))
					return;

				action = CodeAction.Create(
					Strings.UnityObjectCoalescingAssignmentCodeFixTitle,
					ct => ReplaceCoalescingAssignmentAsync(context.Document, aes, ct),
					UnityObjectNullHandlingAnalyzer.CoalescingAssignmentRuleId); // using DiagnosticId as equivalence key for BatchFixer
				break;

			// Pattern expression
			case IsPatternExpressionSyntax pes:
				if (HasSideEffect(pes.Expression))
					return;

				action = CodeAction.Create(
					Strings.UnityObjectIsPatternCodeFixTitle,
					ct => ReplacePatternExpressionAsync(context.Document, pes, ct),
					UnityObjectNullHandlingAnalyzer.IsPatternRuleId); // using DiagnosticId as equivalence key for BatchFixer
				break;

			default:
				return;
		}

		context.RegisterCodeFix(action, context.Diagnostics);
	}

	// We do not want to fix expressions with possible side effects such as `Foo() ?? bar`
	// We could potentially rewrite by introducing a variable
	private static bool HasSideEffect(ExpressionSyntax expression)
	{
		return expression.Kind() switch
		{
			SyntaxKind.SimpleMemberAccessExpression or SyntaxKind.PointerMemberAccessExpression or SyntaxKind.IdentifierName => false,
			_ => true,
		};
	}

	private static async Task<Document> ReplaceWithAsync(Document document, SyntaxNode source, SyntaxNode replacement, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		var newRoot = root?.ReplaceNode(source, replacement);
		return newRoot == null ? document : document.WithSyntaxRoot(newRoot);
	}

	private static Task<Document> ReplaceNullCoalescingAsync(Document document, BinaryExpressionSyntax coalescing, CancellationToken cancellationToken)
	{
		// obj ?? foo -> obj != null ? obj : foo
		var conditional = SyntaxFactory.ConditionalExpression(
			condition: SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, coalescing.Left, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
			whenTrue: coalescing.Left,
			whenFalse: coalescing.Right);

		return ReplaceWithAsync(document, coalescing, conditional, cancellationToken);
	}

	private static Task<Document> ReplaceCoalescingAssignmentAsync(Document document, AssignmentExpressionSyntax coalescing, CancellationToken cancellationToken)
	{
		// obj ??= foo -> obj = obj != null ? obj : foo
		var conditional = SyntaxFactory.ConditionalExpression(
			condition: SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, coalescing.Left, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
			whenTrue: coalescing.Left,
			whenFalse: coalescing.Right);

		var assignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, coalescing.Left, conditional);
		return ReplaceWithAsync(document, coalescing, assignment, cancellationToken);
	}

	private static async Task<Document> ReplaceNullPropagationAsync(Document document, ConditionalAccessExpressionSyntax access, CancellationToken cancellationToken)
	{
		// obj?.member -> obj != null ? obj.member : null
		if (access.WhenNotNull is not MemberBindingExpressionSyntax mbes)
			return document;

		var conditional = SyntaxFactory.ConditionalExpression(
			condition: SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, access.Expression, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
			whenTrue: SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, access.Expression, mbes.Name),
			whenFalse: SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

		return await ReplaceWithAsync(document, access, conditional, cancellationToken);
	}

	private static Task<Document> ReplacePatternExpressionAsync(Document document, IsPatternExpressionSyntax pattern, CancellationToken cancellationToken)
	{
		// obj is null => obj == null, obj is not null => obj != null
		var kind = pattern.Pattern is ConstantPatternSyntax ? SyntaxKind.EqualsExpression : SyntaxKind.NotEqualsExpression;
		var binary = SyntaxFactory.BinaryExpression(kind, pattern.Expression, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

		return ReplaceWithAsync(document, pattern, binary, cancellationToken);
	}
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnityObjectNullHandlingSuppressor : DiagnosticSuppressor
{
	internal static readonly SuppressionDescriptor NullCoalescingRule = new(
		id: "USP0001",
		suppressedDiagnosticId: "IDE0029",
		justification: Strings.UnityObjectNullCoalescingSuppressorJustification);

	internal static readonly SuppressionDescriptor NullPropagationRule = new(
		id: "USP0002",
		suppressedDiagnosticId: "IDE0031",
		justification: Strings.UnityObjectNullPropagationSuppressorJustification);

	internal static readonly SuppressionDescriptor UseIsNullRule = new(
		id: "USP0021",
		suppressedDiagnosticId: "IDE0041",
		justification: Strings.UnityObjectUseIsNullSuppressorJustification);

	internal static readonly SuppressionDescriptor CoalescingAssignmentRule = new(
		id: "USP0017",
		suppressedDiagnosticId: "IDE0074",
		justification: Strings.UnityObjectCoalescingAssignmentSuppressorJustification);

	internal static readonly SuppressionDescriptor IfNullCoalescingRule = new(
		id: "USP0022",
		suppressedDiagnosticId: "IDE0270",
		justification: Strings.UnityObjectIfNullCoalescingSuppressorJustification);

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(NullCoalescingRule, NullPropagationRule, CoalescingAssignmentRule, UseIsNullRule, IfNullCoalescingRule);

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (var diagnostic in context.ReportedDiagnostics)
		{
			AnalyzeDiagnostic(diagnostic, context);
		}
	}

	private void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
	{
		if (diagnostic.Id == UseIsNullRule.SuppressedDiagnosticId)
		{
			var identifierName = context.GetSuppressibleNode<IdentifierNameSyntax>(diagnostic);
			if (identifierName != null)
				AnalyzeIdentifier(diagnostic, context, identifierName);
		}
		else
		{
			var binaryExpression = context.GetSuppressibleNode<BinaryExpressionSyntax>(diagnostic);
			if (binaryExpression != null)
				AnalyzeBinaryExpression(diagnostic, context, binaryExpression);
		}
	}

	private void AnalyzeIdentifier(Diagnostic diagnostic, SuppressionAnalysisContext context, IdentifierNameSyntax identifier)
	{
		if (identifier.Identifier.Text != nameof(ReferenceEquals))
			return;

		var invocation = identifier
			.Ancestors()
			.OfType<InvocationExpressionSyntax>()
			.FirstOrDefault();

		if (invocation == null)
			return;

		var model = context.GetSemanticModel(invocation.SyntaxTree);

		foreach (var argument in invocation.ArgumentList.Arguments)
		{
			var typeInfo = model.GetTypeInfo(argument.Expression);
			ReportSuppressionOnUnityObject(diagnostic, context, typeInfo.Type);
		}
	}

	private void AnalyzeBinaryExpression(Diagnostic diagnostic, SuppressionAnalysisContext context, BinaryExpressionSyntax binaryExpression)
	{
		switch (binaryExpression.Kind())
		{
			case SyntaxKind.EqualsExpression:
			case SyntaxKind.NotEqualsExpression:
				if (!binaryExpression.Right.IsKind(SyntaxKind.NullLiteralExpression))
					return;
				break;

			case SyntaxKind.CoalesceExpression:
				break;

			default:
				return;
		}

		var model = context.GetSemanticModel(binaryExpression.SyntaxTree);
		var typeInfo = model.GetTypeInfo(binaryExpression.Left);

		ReportSuppressionOnUnityObject(diagnostic, context, typeInfo.Type);
	}

	private void ReportSuppressionOnUnityObject(Diagnostic diagnostic, SuppressionAnalysisContext context, ITypeSymbol? type)
	{
		if (type == null)
			return;

		if (!type.Extends(typeof(UnityEngine.Object)))
			return;

		var rule = SupportedSuppressions.FirstOrDefault(r => r.SuppressedDiagnosticId == diagnostic.Id);
		if (rule != null)
			context.ReportSuppression(Suppression.Create(rule, diagnostic));
	}
}
