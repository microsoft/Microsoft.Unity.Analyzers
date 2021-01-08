/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ContextMenuSuppressor : DiagnosticSuppressor
	{
		internal static readonly SuppressionDescriptor ContextMenuRule = new SuppressionDescriptor(
			id: "USP0009",
			suppressedDiagnosticId: "IDE0051",
			justification: Strings.UnusedMethodContextMenuSuppressorJustification);

		internal static readonly SuppressionDescriptor ContextMenuItemUnusedRule = new SuppressionDescriptor(
			id: "USP0010",
			suppressedDiagnosticId: "IDE0051",
			justification: Strings.UnusedContextMenuItemJustification);

		internal static readonly SuppressionDescriptor ContextMenuItemReadonlyRule = new SuppressionDescriptor(
			id: "USP0011",
			suppressedDiagnosticId: "IDE0044",
			justification: Strings.ReadonlyContextMenuItemJustification);

		public override void ReportSuppressions(SuppressionAnalysisContext context)
		{
			foreach (var diagnostic in context.ReportedDiagnostics)
			{
				AnalyzeDiagnostic(diagnostic, context);
			}
		}

		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions =>
			ImmutableArray.Create(ContextMenuRule, ContextMenuItemUnusedRule, ContextMenuItemReadonlyRule);

		private void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
		{
			var location = diagnostic.Location;
			var sourceTree = location.SourceTree;
			var root = sourceTree.GetRoot(context.CancellationToken);
			var node = root.FindNode(location.SourceSpan);
			var model = context.GetSemanticModel(location.SourceTree);
			var symbols = new List<ISymbol>();

			switch (node)
			{
				case MethodDeclarationSyntax method:
					symbols.Add(model.GetDeclaredSymbol(method));
					break;
				case VariableDeclaratorSyntax vdec:
					symbols.Add(model.GetDeclaredSymbol(vdec));
					break;
				case FieldDeclarationSyntax fdec:
					symbols.AddRange(fdec.Declaration.Variables.Select(v => model.GetDeclaredSymbol(v)));
					break;
			}

			var reportableSymbols = symbols
				.Where(IsReportable)
				.ToList();

			if (!reportableSymbols.Any())
				return;

			if (reportableSymbols.FirstOrDefault() is IMethodSymbol)
				context.ReportSuppression(Suppression.Create(ContextMenuRule, diagnostic));
			else
				foreach (var descriptor in SupportedSuppressions.Where(d => d.SuppressedDiagnosticId == diagnostic.Id))
					context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
		}

		private static bool IsReportable(ISymbol symbol)
		{
			if (symbol == null)
				return false;

			var containingType = symbol.ContainingType;

			switch (symbol)
			{
				case IMethodSymbol methodSymbol:
					if (methodSymbol.GetAttributes().Any(a => a.AttributeClass.Matches(typeof(UnityEngine.ContextMenu)) || a.AttributeClass.Matches(typeof(UnityEditor.MenuItem))))
						return true;
					if (IsReferencedByContextMenuItem(methodSymbol, containingType))
						return true;
					break;
				case IFieldSymbol fieldSymbol:
					if (fieldSymbol.GetAttributes().Any(a => a.AttributeClass.Matches(typeof(UnityEngine.ContextMenuItemAttribute))))
						return true;
					break;
			}

			return false;
		}

		private static bool IsReferencedByContextMenuItem(IMethodSymbol symbol, INamedTypeSymbol containingType)
		{
			foreach (var member in containingType.GetMembers())
			{
				if (!(member is IFieldSymbol fieldSymbol))
					continue;

				var attributes = fieldSymbol
					.GetAttributes()
					.Where(a => a.AttributeClass.Matches(typeof(UnityEngine.ContextMenuItemAttribute)))
					.ToArray();

				if (!attributes.Any())
					continue;

				// public ContextMenuItemAttribute(string name, string >>function<<)
				const int methodNameIndex = 1;
				return attributes
					.Select(a => a.ConstructorArguments)
					.Any(ca => ca.Length > methodNameIndex && symbol.Name == ca[methodNameIndex].Value?.ToString());
			}

			return false;
		}
	}
}
