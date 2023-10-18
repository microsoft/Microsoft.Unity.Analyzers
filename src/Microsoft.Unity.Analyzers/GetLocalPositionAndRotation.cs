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

public class GetLocalPositionAndRotationContext : BaseGetPositionAndRotationContext
{
	public static Lazy<BasePositionAndRotationContext> Instance => new(() => new GetLocalPositionAndRotationContext());
	private GetLocalPositionAndRotationContext() : base("localPosition", "localRotation", "GetLocalPositionAndRotation") { }
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GetLocalPositionAndRotationAnalyzer : BasePositionAndRotationAnalyzer
{
	private const string RuleId = "UNT0037";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.GetLocalPositionAndRotationDiagnosticTitle,
		messageFormat: Strings.GetLocalPositionAndRotationDiagnosticMessageFormat,
		category: DiagnosticCategory.Performance,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.GetLocalPositionAndRotationDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

	public GetLocalPositionAndRotationAnalyzer() : base(GetLocalPositionAndRotationContext.Instance.Value) { }
}

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class GetLocalPositionAndRotationCodeFix : BasePositionAndRotationCodeFix
{
	protected override string CodeFixTitle => Strings.GetLocalPositionAndRotationCodeFixTitle;

	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(GetLocalPositionAndRotationAnalyzer.Rule.Id);
	public GetLocalPositionAndRotationCodeFix() : base(GetLocalPositionAndRotationContext.Instance.Value) { }
}
