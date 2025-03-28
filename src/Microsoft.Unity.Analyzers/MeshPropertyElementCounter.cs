/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MeshPropertyElementCounterAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0039";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.MeshPropertyElementCounterDiagnosticTitle,
		messageFormat: Strings.MeshPropertyElementCounterDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.MeshPropertyElementCounterDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterOperationAction(AnalyzeOperation, OperationKind.PropertyReference);
	}

	private void AnalyzeOperation(OperationAnalysisContext context)
	{
		if (context.Operation is not IPropertyReferenceOperation op)
		{
			return;
		}

		var parent = op.Parent;
		if (parent is ISimpleAssignmentOperation or null)
		{
			// not interested in property assignment
			return;
		}

		var prop = op.Property;
		var declaredType = prop.ContainingType;

		// UnityEngine.Mesh
		if (declaredType is not { Name: "Mesh" } ||
			declaredType.ContainingNamespace.Name != "UnityEngine") return;

		if (prop.Name != "vertices")
		{
			return;
		}

		if (parent is IPropertyReferenceOperation parentOp)
		{
			var parentName = parentOp.Property.Name;

			switch (parentName)
			{
				case "Length":
					context.ReportDiagnostic(
						Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), "vertexCount", prop.Name)
					);
					break;
				case "Count":
					{
						if (parent.Parent is not IInvocationOperation callOp)
						{
							return;
						}

						if (callOp.Arguments.Length != 0)
						{
							return;
						}

						// TODO: check if actually System.Linq method
						context.ReportDiagnostic(
							Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), "vertexCount", prop.Name)
						);
						break;
					}
			}
		}
		else if (parent is IInvocationOperation parentCallOp)
		{
			if (parentCallOp.Arguments.Length != 1)
			{
				// don't want to lint on overload that takes predicate
				return;
			}

			var method = parentCallOp.TargetMethod;
			var containingType = method.ContainingType;
			if (containingType == null)
			{
				return;
			}

			string ConstructFullyQualifiedName(INamedTypeSymbol sym)
			{
				var ns = sym.ContainingNamespace;
				var s = new StringBuilder(sym.Name);

				while (ns != null)
				{
					s.Insert(0, ns.Name);
					s.Insert(0, '.');
					ns = sym.ContainingNamespace;
				}

				return s.ToString();
			}

			if (method.Name == "Count" && ConstructFullyQualifiedName(containingType) == "System.Linq.Enumerable")
			{
				context.ReportDiagnostic(
					Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), "vertexCount", prop.Name)
				);
			}
		}
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class MeshPropertyElementCounterCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MeshPropertyElementCounterAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var declaration = await context.GetFixableNodeAsync<MemberAccessExpressionSyntax>();
		if (declaration == null)
			return;

		context.RegisterCodeFix(
			CodeAction.Create(
				Strings.MeshPropertyElementCounterCodeFixTitle,
				ct => SuggestFixAsync(context.Document, declaration, ct),
				declaration.ToFullString()),
			context.Diagnostics);
	}

	private static async Task<Document> SuggestFixAsync(Document document, MemberAccessExpressionSyntax creation, CancellationToken cancellationToken)
	{
		// throw new Exception(creation.ToString());
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

		var receiver = creation.Expression;

		var invocation = MemberAccessExpression(
			SyntaxKind.SimpleMemberAccessExpression,
			receiver,
			IdentifierName(
				Identifier("vertexCount")
			)
		);

		var newRoot = root?.ReplaceNode(creation.Parent, invocation);

		if (newRoot == null)
		{
			return document;
		}

		return document.WithSyntaxRoot(newRoot);
	}
}
