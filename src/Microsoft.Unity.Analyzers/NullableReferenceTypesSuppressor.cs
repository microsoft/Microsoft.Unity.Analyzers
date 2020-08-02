/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

//#nullable enable

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
		private const string NULLABLE_WARNING = "CS8618";

		internal static SuppressionDescriptor NullableRule { get; } = new SuppressionDescriptor("USP0016", NULLABLE_WARNING, Strings.NullableReferenceTypesSuppressorJustification);

		//this, rather than IA => IA.Create(), should be faster because it doesn't allocate a new IA every time the value is needed
		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = ImmutableArray.Create(NullableRule);

		public override void ReportSuppressions(SuppressionAnalysisContext context)
		{
			foreach (Diagnostic diagnostic in context.ReportedDiagnostics.Where(diagnostic => diagnostic.Id == NULLABLE_WARNING))
			{
				SyntaxNode root = diagnostic.Location.SourceTree.GetRoot();
				SyntaxNode location = root.FindNode(diagnostic.Location.SourceSpan);

				ClassDeclarationSyntax classDeclaration = location.FirstAncestorOrSelf<ClassDeclarationSyntax>();

				if (classDeclaration is null)
				{
					continue;
				}

				if (!new ScriptInfo(context.GetSemanticModel(diagnostic.Location.SourceTree).GetDeclaredSymbol(classDeclaration)).HasMessages)
				{
					continue;
				}

				//does the inspected class inherit from Monobehaviour?
				//if (!location.FirstAncestorOrSelf<ClassDeclarationSyntax>().BaseList?.ChildNodes().Any(node => GetSymbol(diagnostic.Location.SourceTree, ((BaseTypeSyntax)node).Type, context.Compilation).Extends(typeof(UnityEngine.MonoBehaviour))) ?? true)
				//{
				//	continue;
				//}

				PropertyDeclarationSyntax propertyDeclaration = location.FirstAncestorOrSelf<PropertyDeclarationSyntax>();

				//handle properties before fields to minimize double checking of potential backing fields
				if (!(propertyDeclaration is null))
				{
					AnalyzeProperties(propertyDeclaration, diagnostic, context, root);
					continue;
				}

				if (location is VariableDeclaratorSyntax fieldDeclaration)
				{
					AnalyzeFields(fieldDeclaration, diagnostic, context, root);
					continue;
				}

				//TODO handle nullable warnings for constructors => diagnostic location is now on constructor
				//TODO handle other Unity objects that cannot be initialized => e.g. Unity's new Input system
				//TODO annotate nullability once this project actively uses C# 8.0
			}
		}

		private static void AnalyzeFields(VariableDeclaratorSyntax declarator, Diagnostic diagnostic, SuppressionAnalysisContext context, SyntaxNode root)
		{
			FieldDeclarationSyntax declarationSyntax = declarator.FirstAncestorOrSelf<FieldDeclarationSyntax>();

			//suppress for fields that are not private and not static => statics cannot be set in editor and are not shown in the inspector and cannot be set there
			if (!declarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PrivateKeyword) || modifier.IsKind(SyntaxKind.StaticKeyword))
				&& !declarationSyntax.AttributeLists.Any(attributeList => attributeList.Attributes.Any(attribute => attribute.Name.ToString() == "HideInInspector")))
			{
				context.ReportSuppression(Suppression.Create(NullableRule, diagnostic));
				return;
			}

			//check for serializefield attribute => variable could be set in editor
			if (declarationSyntax.AttributeLists.Any(attributeList => attributeList.Attributes.Any(attribute => attribute.Name.ToString() == "SerializeField")))
			{
				context.ReportSuppression(Suppression.Create(NullableRule, diagnostic));
				return;
			}

			ITypeSymbol symbol = GetSymbol(diagnostic.Location.SourceTree, declarationSyntax.Declaration.Type, context.Compilation);

			if (symbol.Extends(typeof(UnityEngine.Object)))
			{
				IEnumerable<SyntaxNode> methodBodies = MethodBodies(root);

				//check for valid assignments
				if (IsAssignedTo(declarator.Identifier.Text, methodBodies))
				{
					context.ReportSuppression(Suppression.Create(NullableRule, diagnostic));
					return;
				}

				//check for existence of this variable in any assigned property
				foreach (string variable in AssignedProperties(root, methodBodies))
				{
					if (variable == declarator.Identifier.Text)
					{
						context.ReportSuppression(Suppression.Create(NullableRule, diagnostic));
					}
				}
			}
		}

		private static void AnalyzeProperties(PropertyDeclarationSyntax declarationSyntax, Diagnostic diagnostic, SuppressionAnalysisContext context, SyntaxNode root)
		{
			ITypeSymbol symbol = GetSymbol(diagnostic.Location.SourceTree, declarationSyntax.Type, context.Compilation);

			if (!symbol.Extends(typeof(UnityEngine.Object)))
			{
				return;
			}

			//check for valid assignments
			if (IsAssignedTo(declarationSyntax.Identifier.Text, MethodBodies(root)))
			{
				context.ReportSuppression(Suppression.Create(NullableRule, diagnostic));
			}
		}

		private static ITypeSymbol GetSymbol(SyntaxTree tree, TypeSyntax type, Compilation compilation)
		{
			return (ITypeSymbol)compilation.GetSemanticModel(tree).GetSymbolInfo(type).Symbol;
		}

		//analyze if a property is assigned inside a methodbody
		private static bool IsAssignedTo(string identifier, IEnumerable<SyntaxNode> methodBodies)
		{
			return methodBodies.Select(node => node.DescendantNodes().OfType<AssignmentExpressionSyntax>()
			.Where(assignmentExpression => assignmentExpression.Left.ToString() == identifier && assignmentExpression.Right.ToString() != "null")
			.FirstOrDefault())
				.Any(expression => !(expression is null));
		}

		//get all method bodies from all unity methods and methods called by them
		private static IEnumerable<SyntaxNode> MethodBodies(SyntaxNode root)
		{
			IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
			IEnumerable<SyntaxNode> methodBodies = Enumerable.Empty<SyntaxNode>();

			//get all unity messages usable as initialization messages
			foreach (MethodDeclarationSyntax methodSyntax in methods.Where(method => (method.Identifier.Text == "Start" || method.Identifier.Text == "Awake" || method.Identifier.Text == "OnEnable" || method.Identifier.Text == "OnWizardCreate") && method.ReturnType.ChildTokens().Any(token => token.IsKind(SyntaxKind.VoidKeyword))))
			{
				methodBodies = methodBodies.Concat(methods
					.Where(syntax => methodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>()
					.Any(invocationSyntax => invocationSyntax.Expression.ToString() == syntax.Identifier.Text))
					.Concat(new[] { methodSyntax })
					.Select(method => method.Body ?? (SyntaxNode)method.ExpressionBody));
			}

			return methodBodies;
		}

		//get all explicit backingfields of assigned properties
		private static IEnumerable<string> AssignedProperties(SyntaxNode root, IEnumerable<SyntaxNode> methodBodies)
		{
			return root.DescendantNodes().OfType<PropertyDeclarationSyntax>()
				.Where(property => property.AccessorList.Accessors.Any(accessor => accessor.Keyword.IsKind(SyntaxKind.SetKeyword) && IsAssignedTo(property.Identifier.Text, methodBodies)))
				.SelectMany(syntax => syntax.AccessorList.Accessors
					.Select(accessor => accessor.Body ?? (SyntaxNode)accessor.ExpressionBody))
					.Where(node => !(node is null))
				.SelectMany(body => body.DescendantNodes().OfType<AssignmentExpressionSyntax>()
					.Select(assignment => assignment.Left.ToString()));
		}
	}
}
