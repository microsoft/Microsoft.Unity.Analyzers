/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Unity.Analyzers.Resources;
using UnityEngine;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class Vector3ConversionAnalyzer : BaseVectorConversionAnalyzer
{
	private const string RuleId = "UNT0034";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.Vector3ConversionDiagnosticTitle,
		messageFormat: Strings.Vector3ConversionDiagnosticMessageFormat,
		category: DiagnosticCategory.Readability,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.Vector3ConversionDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	protected override Type FromType => typeof(Vector3);
	protected override Type ToType => typeof(Vector2);

	protected override void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location)
	{
		context.ReportDiagnostic(Diagnostic.Create(Rule, location));
	}

	protected override bool CheckArguments(IObjectCreationOperation ocOperation)
	{
		if (ocOperation.Arguments.Length != 2)
			return false;

		return base.CheckArguments(ocOperation);
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class Vector3ConversionCodeFix : BaseVectorConversionCodeFix
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Vector3ConversionAnalyzer.Rule.Id);

	protected override Type CastType => typeof(Vector2);

	public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		return RegisterCodeFixesAsync(context, Strings.Vector3ConversionCodeFixTitle);
	}
}
