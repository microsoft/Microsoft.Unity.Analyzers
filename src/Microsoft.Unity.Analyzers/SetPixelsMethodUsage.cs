/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;
using UnityEngine;

namespace Microsoft.Unity.Analyzers;

public class SetPixelsMethodUsageAttribute : MethodUsageAttribute
{
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetPixelsMethodUsageAnalyzer : MethodUsageAnalyzer<SetPixelsMethodUsageAttribute>
{
	private const string RuleId = "UNT0017";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.SetPixelsMethodUsageDiagnosticTitle,
		messageFormat: Strings.SetPixelsMethodUsageDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.SetPixelsMethodUsageDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	protected override IEnumerable<MethodInfo> CollectMethods()
	{
		return CollectMethods(typeof(Texture2D), typeof(Texture3D), typeof(Texture2DArray), typeof(CubemapArray));
	}
}
