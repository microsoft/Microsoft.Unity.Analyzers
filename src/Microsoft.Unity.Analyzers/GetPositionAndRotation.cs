/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

public class GetPositionAndRotationContext : BaseGetPositionAndRotationContext
{
	public static Lazy<BasePositionAndRotationContext> Instance => new(() => new GetPositionAndRotationContext());
	private GetPositionAndRotationContext() : base("position", "rotation", "GetPositionAndRotation") { }
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GetPositionAndRotationAnalyzer() : BasePositionAndRotationAnalyzer(GetPositionAndRotationContext.Instance.Value)
{
	private const string RuleId = "UNT0036";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.GetPositionAndRotationDiagnosticTitle,
		messageFormat: Strings.GetPositionAndRotationDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.GetPositionAndRotationDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.SimpleAssignmentExpression);
		context.RegisterSyntaxNodeAction(AnalyzeExpression, SyntaxKind.VariableDeclaration);
	}

	protected override void OnPatternFound(SyntaxNodeAnalysisContext context, StatementSyntax statement)
	{
		context.ReportDiagnostic(Diagnostic.Create(Rule, statement.GetLocation()));
	}
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class GetPositionAndRotationCodeFix() : BasePositionAndRotationCodeFix(GetPositionAndRotationContext.Instance.Value)
{
	protected override string CodeFixTitle => Strings.GetPositionAndRotationCodeFixTitle;

	public sealed override ImmutableArray<string> FixableDiagnosticIds => [GetPositionAndRotationAnalyzer.Rule.Id];
}
