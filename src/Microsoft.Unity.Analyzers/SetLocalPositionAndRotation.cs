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

public class SetLocalPositionAndRotationContext : BaseSetPositionAndRotationContext
{
	public static Lazy<BasePositionAndRotationContext> Instance => new(() => new SetLocalPositionAndRotationContext());
	private SetLocalPositionAndRotationContext() : base("localPosition", "localRotation", "SetLocalPositionAndRotation") { }
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetLocalPositionAndRotationAnalyzer() : BasePositionAndRotationAnalyzer(SetLocalPositionAndRotationContext.Instance.Value)
{
	private const string RuleId = "UNT0032";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.SetLocalPositionAndRotationDiagnosticTitle,
		messageFormat: Strings.SetLocalPositionAndRotationDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.SetLocalPositionAndRotationDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	protected override void OnPatternFound(SyntaxNodeAnalysisContext context, StatementSyntax statement)
	{
		context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class SetLocalPositionAndRotationCodeFix() : BasePositionAndRotationCodeFix(SetLocalPositionAndRotationContext.Instance.Value)
{
	protected override string CodeFixTitle => Strings.SetLocalPositionAndRotationCodeFixTitle;

	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SetLocalPositionAndRotationAnalyzer.Rule.Id);
}
