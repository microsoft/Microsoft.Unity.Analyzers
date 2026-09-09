/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncVoidMethodAnalyzer : DiagnosticAnalyzer
{
	private const string RuleId = "UNT0045";

	internal static readonly DiagnosticDescriptor Rule = new(
		id: RuleId,
		title: Strings.AsyncVoidMethodDiagnosticTitle,
		messageFormat: Strings.AsyncVoidMethodDiagnosticMessageFormat,
		category: DiagnosticCategory.Correctness,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: HelpLink.ForDiagnosticId(RuleId),
		description: Strings.AsyncVoidMethodDiagnosticDescription);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration, SyntaxKind.LocalFunctionStatement);
	}

	private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
	{
		var (symbol, identifier) = context.Node switch
		{
			MethodDeclarationSyntax declaration when declaration.Modifiers.Any(SyntaxKind.AsyncKeyword) =>
				(context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken), declaration.Identifier),
			LocalFunctionStatementSyntax local when local.Modifiers.Any(SyntaxKind.AsyncKeyword) =>
				(context.SemanticModel.GetDeclaredSymbol(local, context.CancellationToken), local.Identifier),
			_ => (null, default(SyntaxToken))
		};

		if (symbol is not IMethodSymbol { IsAsync: true, ReturnsVoid: true } method)
			return;

		if (method.IsOverride || !method.ExplicitInterfaceImplementations.IsEmpty || ImplementsInterfaceMethod(method))
			return;

		if (method.MethodKind == MethodKind.Ordinary && (IsEventHandler(method) || IsUnityCallback(method)))
			return;

		context.ReportDiagnostic(Diagnostic.Create(Rule, identifier.GetLocation(), method.Name));
	}

	private static bool ImplementsInterfaceMethod(IMethodSymbol method)
	{
		return method.ContainingType.AllInterfaces
			.SelectMany(type => type.GetMembers(method.Name))
			.Any(member => SymbolEqualityComparer.Default.Equals(
				method.ContainingType.FindImplementationForInterfaceMember(member), method));
	}

	private static bool IsEventHandler(IMethodSymbol method)
	{
		return method.Parameters.Length == 2
			&& method.Parameters[0] is { RefKind: RefKind.None, Type.SpecialType: SpecialType.System_Object }
			&& method.Parameters[1].RefKind == RefKind.None
			&& method.Parameters[1].Type.Extends(typeof(EventArgs));
	}

	private static bool IsUnityCallback(IMethodSymbol method)
	{
		if (method.Arity != 0)
			return false;

		var scriptInfo = new ScriptInfo(method.ContainingType);
		if (scriptInfo.GetMessages().Any(message => message.IsStatic == method.IsStatic && method.Matches(message)))
			return true;

		if (method.IsStatic && method.Parameters.IsEmpty && LoadAttributeMethodAnalyzer.IsDecorated(method))
			return true;

		return method.GetAttributes().Any(attribute => attribute.AttributeClass != null
			&& (attribute.AttributeClass.Matches(typeof(UnityEngine.ContextMenu))
				|| attribute.AttributeClass.Matches(typeof(UnityEditor.MenuItem))));
	}
}
