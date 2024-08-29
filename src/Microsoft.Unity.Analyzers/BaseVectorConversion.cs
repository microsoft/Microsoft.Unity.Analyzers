/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
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

namespace Microsoft.Unity.Analyzers;

public abstract class BaseVectorConversionAnalyzer : DiagnosticAnalyzer
{
	protected abstract Type FromType { get; }
	protected abstract Type ToType { get; }

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
	}

	private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not ObjectCreationExpressionSyntax ocSyntax)
			return;

		var model = context.SemanticModel;
		if (model.GetOperation(ocSyntax) is not IObjectCreationOperation ocOperation)
			return;

		if (!ocOperation.Type.Matches(ToType))
			return;

		if (!CheckArguments(ocOperation))
			return;

		if (!CheckForNonAmbiguousReplacement(ocOperation, model))
			return;

		ReportDiagnostic(context, ocSyntax.GetLocation());
	}

	protected virtual bool CheckForNonAmbiguousReplacement(IObjectCreationOperation ocOperation, SemanticModel model)
	{
		if (ocOperation.Parent is not IArgumentOperation arOperation)
			return true;

		if (arOperation.Parent is not IInvocationOperation inOperation)
			return true;

		if (inOperation.Syntax is not InvocationExpressionSyntax invocation)
			return true;

		var overloads = model.GetMemberGroup(invocation.Expression);
		return overloads.Length == 1;
	}

	protected virtual bool CheckArguments(IObjectCreationOperation ocOperation)
	{
		if (ocOperation.Arguments.Length < 2)
			return false;

		var first = ocOperation.Arguments[0];
		var second = ocOperation.Arguments[1];

		if (!ArgumentMatches(first, "x"))
			return false;

		if (!ArgumentMatches(second, "y"))
			return false;

		var firstIdentifier = GetIdentifierNameSyntax(first);
		var secondIdentifier = GetIdentifierNameSyntax(second);

		if (firstIdentifier == null || secondIdentifier == null || firstIdentifier.Identifier.Text != secondIdentifier.Identifier.Text)
			return false;

		return true;
	}

	protected abstract void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location);

	private bool ArgumentMatches(IArgumentOperation argumentOperation, string name)
	{
		return argumentOperation.Value is IFieldReferenceOperation fieldOperation
			   && fieldOperation.Field.Name == name
			   && fieldOperation.Field.ContainingType.Matches(FromType);
	}

	internal static IdentifierNameSyntax? GetIdentifierNameSyntax(IArgumentOperation argumentOperation)
	{
		if (argumentOperation.Syntax is not ArgumentSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax identifierNameSyntax } })
			return null;

		return identifierNameSyntax;
	}
}

public abstract class BaseVectorConversionCodeFix : CodeFixProvider
{
	public abstract override ImmutableArray<string> FixableDiagnosticIds { get; }

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public abstract override Task RegisterCodeFixesAsync(CodeFixContext context);

	protected abstract Type CastType { get; }

	protected async Task RegisterCodeFixesAsync(CodeFixContext context, string title)
	{
		var ocSyntax = await context.GetFixableNodeAsync<ObjectCreationExpressionSyntax>();
		if (ocSyntax == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				ct => SimplifyObjectCreationAsync(context.Document, ocSyntax, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private bool IsCastRequired(IObjectCreationOperation ocSyntax)
	{
		if (ocSyntax.Parent is not IArgumentOperation argOperation)
			return true;

		return !argOperation.Parameter.Type.Matches(CastType);
	}

	private async Task<Document> SimplifyObjectCreationAsync(Document document, ObjectCreationExpressionSyntax ocSyntax, CancellationToken ct)
	{
		var root = await document
			.GetSyntaxRootAsync(ct)
			.ConfigureAwait(false);

		var model = await document
			.GetSemanticModelAsync(ct)
			.ConfigureAwait(false);

		if (model?.GetOperation(ocSyntax) is not IObjectCreationOperation ocOperation)
			return document;

		var identifierNameSyntax = BaseVectorConversionAnalyzer.GetIdentifierNameSyntax(ocOperation.Arguments.First());
		if (identifierNameSyntax == null)
			return document;

		var typeSyntax = SyntaxFactory.ParseTypeName(CastType.Name);
		SyntaxNode castedSyntax = IsCastRequired(ocOperation) ? SyntaxFactory.CastExpression(typeSyntax, identifierNameSyntax) : identifierNameSyntax;

		var newRoot = root?.ReplaceNode(ocSyntax, castedSyntax);
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}
}
