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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ShaderPropertyToIDAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0046";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.ShaderPropertyToIDDiagnosticTitle,
		messageFormat: Strings.ShaderPropertyToIDDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.ShaderPropertyToIDDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordDeclaration);
	}

	private static void AnalyzeType(SyntaxNodeAnalysisContext context)
	{
		var groups = CollectCalls((TypeDeclarationSyntax)context.Node, context.SemanticModel, context.CancellationToken);
		if (groups == null)
			return;

		foreach (var group in groups)
		{
			if (group.Value.Count >= 2)
				context.ReportDiagnostic(Diagnostic.Create(Rule, group.Value[0].GetLocation(), group.Key));
		}
	}

	internal static Dictionary<string, List<InvocationExpressionSyntax>>? CollectCalls(
		TypeDeclarationSyntax declaration, SemanticModel model, CancellationToken cancellationToken)
	{
		List<InvocationExpressionSyntax>? candidates = null;
		foreach (var member in declaration.Members)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (member is BaseTypeDeclarationSyntax or BaseFieldDeclarationSyntax)
				continue;

			foreach (var node in member.DescendantNodes(ShouldDescend))
			{
				if (node is not InvocationExpressionSyntax invocation || invocation.ArgumentList.Arguments.Count != 1
					|| invocation.ContainsDirectives)
					continue;

				var name = invocation.Expression switch
				{
					MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
					IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
					_ => null
				};

				if (name == "PropertyToID"
					&& invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal
					&& literal.IsKind(SyntaxKind.StringLiteralExpression))
					(candidates ??= []).Add(invocation);
			}
		}

		if (candidates == null || candidates.Count < 2)
			return null;

		Dictionary<string, List<InvocationExpressionSyntax>>? groups = null;
		foreach (var candidate in candidates)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (model.GetOperation(candidate, cancellationToken) is not IInvocationOperation operation
				|| operation.Parent is IExpressionStatementOperation
				|| operation.Arguments.Length != 1
				|| operation.Arguments[0].Value.ConstantValue.Value is not string value)
				continue;

			var method = operation.TargetMethod;
			if (!method.IsStatic || !method.ContainingType.Matches(typeof(UnityEngine.Shader))
				|| method.Name != "PropertyToID" || method.Parameters.Length != 1
				|| method.Parameters[0].Type.SpecialType != SpecialType.System_String
				|| method.ReturnType.SpecialType != SpecialType.System_Int32)
				continue;

			groups ??= new(StringComparer.Ordinal);
			if (!groups.TryGetValue(value, out var calls))
				groups.Add(value, calls = []);
			calls.Add(candidate);
		}

		return groups;
	}

	private static bool ShouldDescend(SyntaxNode node)
	{
		return node is not EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax };
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class ShaderPropertyToIDCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ShaderPropertyToIDAnalyzer.Rule.Id);

	// Independent batch actions cannot coordinate generated fields in the same type.
	public sealed override FixAllProvider? GetFixAllProvider() => null;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var invocation = await context.GetFixableNodeAsync<InvocationExpressionSyntax>();
		if (invocation == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.ShaderPropertyToIDCodeFixTitle,
				ct => CachePropertyIdAsync(context.Document, invocation, ct),
				ShaderPropertyToIDAnalyzer.Rule.Id),
			context.Diagnostics);
	}

	private static async Task<Document> CachePropertyIdAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
	{
		var declaration = invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>();
		var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (declaration == null || model?.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol factory)
			return document;

		var groups = ShaderPropertyToIDAnalyzer.CollectCalls(declaration, model, cancellationToken);
		if (groups == null)
			return document;

		foreach (var group in groups)
		{
			if (group.Value.Count < 2 || !group.Value.Contains(invocation))
				continue;

			var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
			var fieldName = CachedStringIdField.GetOrCreate(editor, declaration, factory, group.Key, "Id", group.Value, cancellationToken);
			if (fieldName == null)
				return document;

			foreach (var call in group.Value)
			{
				var interiorTrivia = call.DescendantTrivia().Where(trivia => call.Span.Contains(trivia.Span));
				var reference = IdentifierName(fieldName)
					.WithTriviaFrom(call)
					.WithLeadingTrivia(call.GetLeadingTrivia().AddRange(interiorTrivia))
					.WithAdditionalAnnotations(Formatter.Annotation);
				editor.ReplaceNode(call, reference);
			}

			return editor.GetChangedDocument();
		}

		return document;
	}
}
