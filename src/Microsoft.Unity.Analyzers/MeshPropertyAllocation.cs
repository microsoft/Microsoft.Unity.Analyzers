/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MeshPropertyAllocationAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0038";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.MeshPropertyAllocationDiagnosticTitle,
		messageFormat: Strings.MeshPropertyAllocationDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.MeshPropertyAllocationDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterOperationAction(AnalyzeOperation, (OperationKind[])Enum.GetValues(typeof(OperationKind)));
	}

	private void AnalyzeOperation(OperationAnalysisContext context)
	{
		if (context.Operation is not IPropertyReferenceOperation op)
		{
			return;
		}

		if (op.Parent is ISimpleAssignmentOperation)
		{
			// not interested in property assignment
			return;
		}

		var prop = op.Property;
		var declaredType = prop.ContainingType;

		// UnityEngine.Mesh
		if (declaredType is not { Name: "Mesh" } ||
			declaredType.ContainingNamespace.Name != "UnityEngine") return;

		// properties that allocates copy of corresponding data in their getter
		if (prop.Name is not ("uv" or "uv2" or "uv3" or "uv4" or "uv5" or "uv6" or "uv7" or "vertices" or "color" or "color32")) return;

		// TODO: check if the receiver is being overwrite or its property is changed by loop-dependant way:
		//       Example: the following code should not be linted.
		//       ```
		//       // IEnumerable<UnityEngine.Mesh> meshes = /* ... */
		//       foreach (var mesh in meshes) {
		//           var uv = mesh.uv;
		//           // whatever
		//       }
		//       ```

		var isInLoop = op.Syntax
			.Ancestors()
			.Any(x => x is ForStatementSyntax or WhileStatementSyntax or DoStatementSyntax or ForEachStatementSyntax);

		if (!isInLoop) return;

		context.ReportDiagnostic(
			Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), prop.Name)
		);
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class MeshPropertyAllocationCodeFix : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MeshPropertyAllocationAnalyzer.Rule.Id);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		// TODO: provide auto fix. This should be simple: move getting allocating-property to outer scope should be sufficient.
		return Task.CompletedTask;
	}
}
