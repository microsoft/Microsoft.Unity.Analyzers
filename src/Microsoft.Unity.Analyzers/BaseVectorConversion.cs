/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;

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

		ReportDiagnostic(context, ocSyntax.GetLocation());
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
				ocSyntax.ToFullString()),
			context.Diagnostics);
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
		
		// Tag the syntax node with the Simplifier annotation, to remove the redundant cast when possible.
		var castedSyntax = SyntaxFactory
			.CastExpression(typeSyntax, identifierNameSyntax)
			.WithAdditionalAnnotations(Simplifier.Annotation);
		
		var newRoot = root?.ReplaceNode(ocSyntax, castedSyntax);
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}
}
