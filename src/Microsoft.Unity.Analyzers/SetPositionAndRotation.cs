/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

public class SetPositionAndRotationContext : BaseSetPositionAndRotationContext
{
	public static Lazy<BasePositionAndRotationContext> Instance => new(() => new SetPositionAndRotationContext());
	private SetPositionAndRotationContext() : base("position", "rotation", "SetPositionAndRotation") { }
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetPositionAndRotationAnalyzer() : BasePositionAndRotationAnalyzer(SetPositionAndRotationContext.Instance.Value)
{
	private const string RuleId = "UNT0022";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.SetPositionAndRotationDiagnosticTitle,
		messageFormat: Strings.SetPositionAndRotationDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.SetPositionAndRotationDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	protected override void OnPatternFound(SyntaxNodeAnalysisContext context, StatementSyntax statement)
	{
		context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class SetPositionAndRotationCodeFix() : BasePositionAndRotationCodeFix(SetPositionAndRotationContext.Instance.Value)
{
	protected override string CodeFixTitle => Strings.SetPositionAndRotationCodeFixTitle;

	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SetPositionAndRotationAnalyzer.Rule.Id);
}
