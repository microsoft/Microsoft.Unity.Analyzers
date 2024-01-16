/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;
using UnityEngine;

namespace Microsoft.Unity.Analyzers;

public class PhysicsAllocMethodUsageAttribute : MethodUsageAttribute;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PhysicsAllocMethodUsageAnalyzer : MethodUsageAnalyzer<PhysicsAllocMethodUsageAttribute>
{
	private const string RuleId = "UNT0028";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.PhysicsAllocMethodUsageDiagnosticTitle,
		messageFormat: Strings.PhysicsAllocMethodUsageDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.PhysicsAllocMethodUsageDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	protected override IEnumerable<MethodInfo> CollectMethods()
	{
		return CollectMethods(typeof(Physics));
	}

	protected override void ReportDiagnostic(SyntaxNodeAnalysisContext context, MemberAccessExpressionSyntax member, IMethodSymbol method)
	{
		var candidate = method.Name.Replace("All", string.Empty) + "NonAlloc";
		context.ReportDiagnostic(Diagnostic.Create(Rule, member.Name.GetLocation(), method.Name, candidate));
	}
}
