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

public class SetLocalPositionAndRotationContext : BasePositionAndRotationContext
{
	public static Lazy<BasePositionAndRotationContext> Instance => new(() => new SetLocalPositionAndRotationContext());
	private SetLocalPositionAndRotationContext() : base("localPosition", "localRotation", "SetLocalPositionAndRotation") { }
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetLocalPositionAndRotationAnalyzer : BasePositionAndRotationAnalyzer
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

	protected override void OnPatternFound(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignmentExpression)
	{
		context.ReportDiagnostic(Diagnostic.Create(Rule, assignmentExpression.GetLocation()));
	}

	public SetLocalPositionAndRotationAnalyzer() : base(SetLocalPositionAndRotationContext.Instance.Value) { }
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class SetLocalPositionAndRotationCodeFix : BasePositionAndRotationCodeFix
{
	protected override string CodeFixTitle => Strings.SetLocalPositionAndRotationCodeFixTitle;
	
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SetLocalPositionAndRotationAnalyzer.Rule.Id);
	public SetLocalPositionAndRotationCodeFix() : base(SetLocalPositionAndRotationContext.Instance.Value) { }
}
