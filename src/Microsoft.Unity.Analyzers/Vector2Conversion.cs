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
public class Vector2ConversionAnalyzer : BaseVectorConversionAnalyzer
{
	internal static readonly DiagnosticDescriptor Rule = new(
		id: "UNT0035",
		title: Strings.Vector3ConversionDiagnosticTitle,
		messageFormat: Strings.Vector3ConversionDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: Strings.Vector3ConversionDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	protected override Type FromType => typeof(Vector2);
	protected override Type ToType => typeof(Vector3);

	protected override void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location)
	{
		context.ReportDiagnostic(Diagnostic.Create(Rule, location));
	}

	protected override bool CheckArguments(IObjectCreationOperation ocOperation)
	{
		if (ocOperation.Arguments.Length != 3)
			return false;

		if (ocOperation.Arguments[2] is not { } third)
			return false;

		if (!third.Value.ConstantValue.HasValue)
			return false;

		if (third.Value.ConstantValue.Value is not 0f)
			return false;

		return base.CheckArguments(ocOperation);
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class Vector2ConversionCodeFix : BaseVectorConversionCodeFix
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Vector2ConversionAnalyzer.Rule.Id);

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		await RegisterCodeFixesAsync(context, Strings.Vector2ConversionCodeFixTitle)
			.ConfigureAwait(false);
	}

	protected override Type CastType => typeof(Vector3);
}
