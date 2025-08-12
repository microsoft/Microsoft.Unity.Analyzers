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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CacheYieldInstructionAnalyzerAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0038";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.CacheYieldInstructionAnalyzerDiagnosticTitle,
		messageFormat: Strings.CacheYieldInstructionAnalyzerDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.CacheYieldInstructionAnalyzerDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeYieldReturn, SyntaxKind.YieldReturnStatement);
	}

	private static readonly string[] _cachableTypes =
	[
		"UnityEngine.WaitForSeconds",
		"UnityEngine.WaitForSecondsRealtime"
	];

	private static void AnalyzeYieldReturn(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not YieldStatementSyntax yieldStatement)
			return;

		if (yieldStatement.Expression is not ObjectCreationExpressionSyntax objectCreation)
			return;

		if (objectCreation.ArgumentList?.Arguments.SingleOrDefault(arg => arg.Expression is LiteralExpressionSyntax) == null)
			return;

		var typeSymbol = context.SemanticModel.GetTypeInfo(objectCreation).Type;
		if (typeSymbol == null)
			return;

		var typeName = typeSymbol.ToDisplayString();
		if (!_cachableTypes.Contains(typeName))
			return;

		context.ReportDiagnostic(Diagnostic.Create(
			Rule,
			objectCreation.GetLocation(),
			typeSymbol.Name
		));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class CacheYieldInstructionAnalyzerCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CacheYieldInstructionAnalyzerAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var objectCreation = await context.GetFixableNodeAsync<ObjectCreationExpressionSyntax>();
		if (objectCreation == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.CacheYieldInstructionAnalyzerCodeFixTitle,
				ct => CacheYieldInstructionAsync(context.Document, objectCreation, ct),
				FixableDiagnosticIds.Single()), // using DiagnosticId as equivalence key for BatchFixer
			context.Diagnostics);
	}

	private static async Task<Document> CacheYieldInstructionAsync(Document document, ObjectCreationExpressionSyntax objectCreation, CancellationToken cancellationToken)
	{
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		if (semanticModel == null)
			return document;

		var method = objectCreation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
		var classDecl = method?.FirstAncestorOrSelf<ClassDeclarationSyntax>();
		if (classDecl == null)
			return document;

		var type = semanticModel.GetTypeInfo(objectCreation).Type;
		if (type == null)
			return document;

		var fieldName = GenerateFieldName(objectCreation, type);
		if (fieldName == null)
			return document;

		var fieldExists = classDecl.Members
			.OfType<FieldDeclarationSyntax>()
			.SelectMany(f => f.Declaration.Variables)
			.Any(v => v.Identifier.Text == fieldName);

		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
		if (!fieldExists)
		{
			var typeName = type.ToMinimalDisplayString(semanticModel, objectCreation.SpanStart);

			var fieldDecl = SyntaxFactory.FieldDeclaration(
				SyntaxFactory.VariableDeclaration(
					SyntaxFactory.ParseTypeName(typeName),
					SyntaxFactory.SeparatedList([
						SyntaxFactory.VariableDeclarator(
							SyntaxFactory.Identifier(fieldName),
							null,
							SyntaxFactory.EqualsValueClause(objectCreation.WithoutTrivia()))
					])
				)
			)
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

			editor.InsertMembers(classDecl, 0, [fieldDecl]);
		}

		var yieldStatement = objectCreation.FirstAncestorOrSelf<YieldStatementSyntax>();
		if (yieldStatement == null)
			return document;

		var newYield = yieldStatement
			.WithExpression(SyntaxFactory.IdentifierName(fieldName))
			.WithTriviaFrom(yieldStatement);

		editor.ReplaceNode(yieldStatement, newYield);
		return editor.GetChangedDocument();
	}

	private static string? GenerateFieldName(ObjectCreationExpressionSyntax objectCreation, ITypeSymbol type)
	{
		var argList = objectCreation.ArgumentList;
		if (argList?.Arguments.Count != 1)
			return null;

		var argument = argList.Arguments.ToFullString().Trim();
		var fieldName = $"_{type.Name.Substring(0, 1).ToLower()}{type.Name.Substring(1)}{NormalizeName(argument)}";
		return fieldName;
	}

	private static string NormalizeName(string name)
	{
		return name
			.Replace(".", "_")
			.Replace(",", "_")
			.Replace("f", "")
			.Replace(" ", "");
	}
}
