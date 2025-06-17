/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SerializedFieldSuppressor : BaseAttributeSuppressor
{
	internal static readonly SuppressionDescriptor ReadonlyRule = new(
		id: "USP0004",
		suppressedDiagnosticId: "IDE0044",
		justification: Strings.ReadonlySerializedFieldSuppressorJustification);

	internal static readonly SuppressionDescriptor UnusedRule = new(
		id: "USP0006",
		suppressedDiagnosticId: "IDE0051",
		justification: Strings.UnusedSerializedFieldSuppressorJustification);

	internal static readonly SuppressionDescriptor UnusedCodeQualityRule = new(
		id: "USP0013",
		suppressedDiagnosticId: "CA1823",
		justification: Strings.UnusedSerializedFieldSuppressorJustification);

	internal static readonly SuppressionDescriptor NeverAssignedRule = new(
		id: "USP0007",
		suppressedDiagnosticId: "CS0649",
		justification: Strings.NeverAssignedSerializedFieldSuppressorJustification);

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [
		ReadonlyRule,
		UnusedRule,
		UnusedCodeQualityRule,
		NeverAssignedRule
	];

	protected override Type[] SuppressableAttributeTypes =>
	[
		typeof(UnityEngine.SerializeField),
		typeof(UnityEngine.SerializeReference),
		typeof(Sirenix.Serialization.OdinSerializeAttribute)
	];

	protected override bool IsSuppressable(ISymbol symbol)
	{
		if (base.IsSuppressable(symbol))
			return true;

		return symbol.DeclaredAccessibility == Accessibility.Public && symbol.ContainingType.Extends(typeof(UnityEngine.Object));
	}

	protected override SyntaxNode? GetSuppressibleNode(Diagnostic diagnostic, SuppressionAnalysisContext context)
	{
		return context.GetSuppressibleNode<VariableDeclaratorSyntax>(diagnostic);
	}
}
