/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NullableReferenceTypesSuppressor : DiagnosticSuppressor
	{
		internal static readonly SuppressionDescriptor Rule = new(
			id: "USP0016",
			suppressedDiagnosticId: "CS8618",
			justification: Strings.NullableReferenceTypesSuppressorJustification);

		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(Rule);

		public override void ReportSuppressions(SuppressionAnalysisContext context)
		{
			foreach (var diagnostic in context.ReportedDiagnostics)
			{
				var root = diagnostic.Location.SourceTree.GetRoot();
				var node = root?.FindNode(diagnostic.Location.SourceSpan);

				var classDeclaration = node?.FirstAncestorOrSelf<ClassDeclarationSyntax>();
				if (classDeclaration is null)
					continue;

				var model = context.GetSemanticModel(diagnostic.Location.SourceTree);
				var symbol = model?.GetDeclaredSymbol(classDeclaration);
				if (symbol is null)
					continue;

				var scriptInfo = new ScriptInfo(symbol);
				if (!scriptInfo.HasMessages)
					continue;

				var propertyDeclaration = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();

				//handle properties before fields to minimize double checking of potential backing fields
				if (!(propertyDeclaration is null))
				{
					AnalyzeProperties(propertyDeclaration, diagnostic, context, root);
					continue;
				}

				var fieldDeclaration = context.GetSuppressibleNode<VariableDeclaratorSyntax>(diagnostic);
				if (fieldDeclaration != null)
					AnalyzeFields(fieldDeclaration, diagnostic, context, root);

				//TODO handle nullable warnings for constructors => diagnostic location is now on constructor
				//TODO handle other Unity objects that cannot be initialized => e.g. Unity's new Input system
			}
		}

		private static void AnalyzeFields(VariableDeclaratorSyntax declarator, Diagnostic diagnostic, SuppressionAnalysisContext context, SyntaxNode root)
		{
			var declarationSyntax = declarator.FirstAncestorOrSelf<FieldDeclarationSyntax>();

			//suppress for fields that are not private and not static => statics cannot be set in editor and are not shown in the inspector and cannot be set there
			if (!declarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PrivateKeyword) || modifier.IsKind(SyntaxKind.StaticKeyword))
				&& !declarationSyntax.AttributeLists.Any(attributeList => attributeList.Attributes.Any(attribute => attribute.Name.ToString() == nameof(UnityEngine.HideInInspector))))
			{
				context.ReportSuppression(Suppression.Create(Rule, diagnostic));
				return;
			}

			//check for serializefield attribute => variable could be set in editor
			if (declarationSyntax.AttributeLists.Any(attributeList => attributeList.Attributes.Any(attribute => attribute.Name.ToString() == nameof(UnityEngine.SerializeField))))
			{
				context.ReportSuppression(Suppression.Create(Rule, diagnostic));
				return;
			}

			var symbol = GetSymbol(diagnostic.Location.SourceTree, declarationSyntax.Declaration.Type, context);

			if (!symbol.Extends(typeof(UnityEngine.Object)))
				return;

			var methodBodies = MethodBodies(root)
				.ToList();

			//check for valid assignments
			if (IsAssignedTo(declarator.Identifier.Text, methodBodies))
			{
				context.ReportSuppression(Suppression.Create(Rule, diagnostic));
				return;
			}

			//check for existence of this variable in any assigned property
			foreach (string variable in AssignedProperties(root, methodBodies))
			{
				if (variable == declarator.Identifier.Text)
					context.ReportSuppression(Suppression.Create(Rule, diagnostic));
			}
		}

		private static void AnalyzeProperties(PropertyDeclarationSyntax declarationSyntax, Diagnostic diagnostic, SuppressionAnalysisContext context, SyntaxNode root)
		{
			var symbol = GetSymbol(diagnostic.Location.SourceTree, declarationSyntax.Type, context);

			if (!symbol.Extends(typeof(UnityEngine.Object)))
				return;

			//check for valid assignments
			if (IsAssignedTo(declarationSyntax.Identifier.Text, MethodBodies(root)))
				context.ReportSuppression(Suppression.Create(Rule, diagnostic));
		}

		private static ITypeSymbol GetSymbol(CodeAnalysis.SyntaxTree tree, TypeSyntax type, SuppressionAnalysisContext context)
		{
			var model = context.GetSemanticModel(tree);
			return (ITypeSymbol)model.GetSymbolInfo(type).Symbol;
		}

		//analyze if a property is assigned inside a methodbody
		private static bool IsAssignedTo(string identifier, IEnumerable<SyntaxNode> methodBodies)
		{
			return methodBodies.Select(node => node.DescendantNodes()
					.OfType<AssignmentExpressionSyntax>()
					.FirstOrDefault(assignmentExpression => assignmentExpression.Left.ToString() == identifier && assignmentExpression.Right.ToString() != "null"))
				.Any(expression => !(expression is null));
		}

		//get all method bodies from all unity methods and methods called by them
		private static IEnumerable<SyntaxNode> MethodBodies(SyntaxNode root)
		{
			var methods = root
				.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.ToList();

			var methodBodies = Enumerable.Empty<SyntaxNode>();

			//get all unity messages usable as initialization messages
			foreach (var methodSyntax in methods.Where(IsInitilizationMessage))
			{
				methodBodies = methodBodies.Concat(methods
					.Where(syntax => methodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>()
						.Any(invocationSyntax => invocationSyntax.Expression.ToString() == syntax.Identifier.Text))
					.Concat(new[] { methodSyntax })
					.Select(method => method.Body ?? (SyntaxNode)method.ExpressionBody));
			}

			return methodBodies;
		}

		private static bool IsInitilizationMessage(MethodDeclarationSyntax method)
		{
			return method.Identifier.Text switch
			{
				"Start" or "Awake" or "OnEnable" or "OnWizardCreate" => method.ReturnType.ChildTokens().Any(token => token.IsKind(SyntaxKind.VoidKeyword)),
				_ => false,
			};
		}

		//get all explicit backingfields of assigned properties
		private static IEnumerable<string> AssignedProperties(SyntaxNode root, IEnumerable<SyntaxNode> methodBodies)
		{
			return root.DescendantNodes().OfType<PropertyDeclarationSyntax>()
				.Where(property => property.AccessorList != null && property.AccessorList.Accessors.Any(accessor => accessor.Keyword.IsKind(SyntaxKind.SetKeyword) && IsAssignedTo(property.Identifier.Text, methodBodies)))
				.SelectMany(syntax => syntax.AccessorList.Accessors
					.Select(accessor => accessor.Body ?? (SyntaxNode)accessor.ExpressionBody))
				.Where(node => !(node is null))
				.SelectMany(body => body.DescendantNodes().OfType<AssignmentExpressionSyntax>()
					.Select(assignment => assignment.Left.ToString()));
		}
	}
}
