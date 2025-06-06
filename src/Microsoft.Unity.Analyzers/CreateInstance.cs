﻿/*--------------------------------------------------------------------------------------------
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

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CreateInstanceAnalyzer : DiagnosticAnalyzer
{
	public const string ComponentId = "UNT0010";

	internal static readonly DiagnosticDescriptor ComponentIdRule = new(
		ComponentId,
		title: Strings.CreateComponentInstanceDiagnosticTitle,
		messageFormat: Strings.CreateComponentInstanceDiagnosticMessageFormat,
		category: DiagnosticCategory.TypeSafety,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(ComponentId),
		description: Strings.CreateMonoBehaviourInstanceDiagnosticDescription);

	public const string ScriptableObjectId = "UNT0011";

	internal static readonly DiagnosticDescriptor ScriptableObjectRule = new(
		ScriptableObjectId,
		title: Strings.CreateScriptableObjectInstanceDiagnosticTitle,
		messageFormat: Strings.CreateScriptableObjectInstanceDiagnosticMessageFormat,
		category: DiagnosticCategory.TypeSafety,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(ScriptableObjectId),
		description: Strings.CreateScriptableObjectInstanceDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ComponentIdRule, ScriptableObjectRule];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
	}

	private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not ObjectCreationExpressionSyntax creation)
			return;

		var typeInfo = context.SemanticModel.GetTypeInfo(creation);
		if (typeInfo.Type == null)
			return;

		if (typeInfo.Type.Extends(typeof(UnityEngine.ScriptableObject)))
		{
			context.ReportDiagnostic(Diagnostic.Create(ScriptableObjectRule, creation.GetLocation(), typeInfo.Type.Name));
			return;
		}

		if (!typeInfo.Type.Extends(typeof(UnityEngine.Component)))
			return;

		context.ReportDiagnostic(Diagnostic.Create(ComponentIdRule, creation.GetLocation(), typeInfo.Type.Name));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class CreateInstanceCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => [CreateInstanceAnalyzer.ComponentId, CreateInstanceAnalyzer.ScriptableObjectId];

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var creation = await context.GetFixableNodeAsync<ObjectCreationExpressionSyntax>();
		if (creation == null)
			return;

		var diagnostic = context.Diagnostics.FirstOrDefault();
		if (diagnostic == null)
			return;

		var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
		if (model == null)
			return;

		switch (diagnostic.Id)
		{
			case CreateInstanceAnalyzer.ScriptableObjectId:
				context.RegisterCodeFix(
					CodeAction.Create(
						Strings.CreateScriptableObjectInstanceCodeFixTitle,
						ct => ReplaceWithInvocationAsync(context.Document, creation, "ScriptableObject", "CreateInstance", ct),
						diagnostic.Id), // using DiagnosticId as equivalence key for BatchFixer
					context.Diagnostics);
				break;
			case CreateInstanceAnalyzer.ComponentId when !IsInsideComponent(creation, model):
				return;
			case CreateInstanceAnalyzer.ComponentId:
				context.RegisterCodeFix(
					CodeAction.Create(
						Strings.CreateMonoBehaviourInstanceCodeFixTitle,
						ct => ReplaceWithInvocationAsync(context.Document, creation, "gameObject", "AddComponent", ct),
						diagnostic.Id), // using DiagnosticId as equivalence key for BatchFixer
					context.Diagnostics);
				break;
		}
	}

	private static bool IsInsideComponent(ObjectCreationExpressionSyntax creation, SemanticModel model)
	{
		var classDeclaration = creation
			.Ancestors()
			.OfType<ClassDeclarationSyntax>()
			.FirstOrDefault();

		if (classDeclaration == null)
			return false;

		var symbol = model.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
		return symbol.Extends(typeof(UnityEngine.Component));
	}

	private static async Task<Document> ReplaceWithInvocationAsync(Document document, ObjectCreationExpressionSyntax creation, string identifierName, string genericMethodName, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

		var typeInfo = semanticModel.GetTypeInfo(creation);
		if (typeInfo.Type == null)
			return document;

		var invocation = InvocationExpression(
			MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				IdentifierName(identifierName),
				GenericName(Identifier(genericMethodName))
					.WithTypeArgumentList(
						TypeArgumentList(
							SingletonSeparatedList<TypeSyntax>(
								IdentifierName(typeInfo.Type.Name))))));

		var newRoot = root?.ReplaceNode(creation, invocation);
		if (newRoot == null)
			return document;

		return document.WithSyntaxRoot(newRoot);
	}
}
