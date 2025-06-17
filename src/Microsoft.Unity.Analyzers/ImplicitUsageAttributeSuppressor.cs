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
using Unity.Properties;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ImplicitUsageAttributeSuppressor : BaseAttributeSuppressor
{
	internal static readonly SuppressionDescriptor Rule = new(
		id: "USP0019",
		suppressedDiagnosticId: "IDE0051",
		justification: Strings.ImplicitUsageAttributeSuppressorJustification);

	protected override Type[] SuppressableAttributeTypes =>
	[
		typeof(JetBrains.Annotations.UsedImplicitlyAttribute),
		typeof(UnityEngine.Scripting.PreserveAttribute),
		typeof(CreatePropertyAttribute)
	];

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [Rule];

	protected override SyntaxNode? GetSuppressibleNode(Diagnostic diagnostic, SuppressionAnalysisContext context)
	{
		return context.GetSuppressibleNode<MemberDeclarationSyntax>(diagnostic) as SyntaxNode
			?? context.GetSuppressibleNode<VariableDeclaratorSyntax>(diagnostic);
	}

	protected override bool IsSuppressableAttribute(INamedTypeSymbol? symbol)
	{
		// The Unity code stripper will consider any attribute with the exact name "PreserveAttribute", regardless of the namespace or assembly
		return base.IsSuppressableAttribute(symbol) || symbol is { Name: nameof(UnityEngine.Scripting.PreserveAttribute) };
	}
}
