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
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.Unity.Analyzers.Resources;
using UnityEngine;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequireComponentAnalyzer : BaseGetComponentAnalyzer
{
	private const string RuleId = "UNT0039";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.RequireComponentAnalyzerDiagnosticTitle,
		messageFormat: Strings.RequireComponentAnalyzerDiagnosticMessageFormat,
		category: DiagnosticCategory.TypeSafety,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.RequireComponentAnalyzerDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;

		var model = context.SemanticModel;
		if (!IsGenericGetComponent(invocation, model, out var method))
			return;

		if (model.GetOperation(invocation) is not IInvocationOperation invocationOperation)
			return;

		if (invocationOperation.Instance is not IInstanceReferenceOperation referenceOperation)
			return;

		var containerType = referenceOperation.Type;
		if (containerType == null)
			return;

		if (!containerType.Extends(typeof(MonoBehaviour)))
			return;

		var componentType = method.TypeArguments.First(); // Checked by IsGenericGetComponent
		if (IsTypeAlreadyRequired(containerType, componentType))
			return;

		if (IsInvocationNullChecked(invocation))
			return;

		var invocationParent = invocation.Parent;
		if (invocationParent == null)
			return;

		if (TryGetTargetdentifier(invocationParent, out var targetIdentifier)
			&& TryGetNextTopNode(invocationParent, out var ifNode)
			&& TryGetConditionIdentifier(ifNode, out var conditionIdentifier, out _, out _)
			&& conditionIdentifier.Value.Text != targetIdentifier.Value.Text)
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
	}

	private static bool IsTypeAlreadyRequired(ITypeSymbol containerType, ITypeSymbol componentType)
	{
		var typeSymbols = containerType
			.GetAttributes()
			.Where(a => a.AttributeClass != null && a.AttributeClass.Matches(typeof(RequireComponent)))
			.SelectMany(a => a.ConstructorArguments)
			.Select(ca => ca.Value)
			.OfType<ISymbol>();

		return typeSymbols.Any(ts => SymbolEqualityComparer.Default.Equals(ts, componentType));
	}

	private static bool IsInvocationNullChecked(InvocationExpressionSyntax invocation)
	{
		var parent = invocation.Parent;
		return parent switch
		{
			BinaryExpressionSyntax binary when
				(binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
				&& (binary.Right.IsKind(SyntaxKind.NullLiteralExpression) || binary.Left.IsKind(SyntaxKind.NullLiteralExpression)) => true,
			_ => false,
		};
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class RequireComponentCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RequireComponentAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var invocation = await context.GetFixableNodeAsync<InvocationExpressionSyntax>();
		if (invocation == null)
			return;

		var classDeclaration = invocation.FirstAncestorOrSelf<ClassDeclarationSyntax>();
		if (classDeclaration == null)
			return;

		var model = await context.Document.GetSemanticModelAsync(context.CancellationToken);
		if (model == null)
			return;

		var symbol = model.GetSymbolInfo(invocation);
		if (symbol.Symbol is not IMethodSymbol method)
			return;

		var typeName = method.TypeArguments.First().ToDisplayString();

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.RequireComponentAnalyzerCodeFixTitle,
				ct => AddRequiredComponentAsync(context.Document, classDeclaration, typeName, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Document> AddRequiredComponentAsync(Document document, ClassDeclarationSyntax classDeclaration, string typeName, CancellationToken cancellationToken)
	{
		var attribute = SyntaxFactory.Attribute(
			SyntaxFactory.ParseName(typeof(RequireComponent).FullName!),
			SyntaxFactory.AttributeArgumentList(
				SyntaxFactory.SingletonSeparatedList(
					SyntaxFactory.AttributeArgument(
						SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(typeName))))))
			.WithAdditionalAnnotations(Simplifier.Annotation);

		var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));

		var leadingTrivia = classDeclaration.GetLeadingTrivia();
		var newClassDeclaration = classDeclaration
			.WithoutLeadingTrivia()
			.AddAttributeLists(attributeList.WithLeadingTrivia(leadingTrivia));

		var root = await document.GetSyntaxRootAsync(cancellationToken);
		if (root == null)
			return document;

		var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
		return document.WithSyntaxRoot(newRoot);
	}
}
