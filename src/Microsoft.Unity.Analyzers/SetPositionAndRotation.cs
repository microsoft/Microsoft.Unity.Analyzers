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
	public static Lazy<BaseSetPositionAndRotationContext> Instance => new(() => new SetPositionAndRotationContext());
	private SetPositionAndRotationContext() : base("position", "rotation", "SetPositionAndRotation") { }
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetPositionAndRotationAnalyzer : BaseSetPositionAndRotationAnalyzer
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

	protected override void OnPatternFound(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignmentExpression)
	{
		context.ReportDiagnostic(Diagnostic.Create(Rule, assignmentExpression.GetLocation()));
	}

	public SetPositionAndRotationAnalyzer() : base(SetPositionAndRotationContext.Instance.Value) { }
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class SetPositionAndRotationCodeFix : BaseSetPositionAndRotationCodeFix
{
	protected override string CodeFixTitle => Strings.SetPositionAndRotationCodeFixTitle;

	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SetPositionAndRotationAnalyzer.Rule.Id);
	public SetPositionAndRotationCodeFix() : base(SetPositionAndRotationContext.Instance.Value) { }
}
