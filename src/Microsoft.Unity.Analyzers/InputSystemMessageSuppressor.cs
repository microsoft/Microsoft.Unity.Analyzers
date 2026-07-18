/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InputSystemMessageSuppressor : DiagnosticSuppressor
{
	internal static readonly SuppressionDescriptor MethodRule = new(
		id: "USP0024",
		suppressedDiagnosticId: "IDE0051",
		justification: Strings.InputSystemMessageSuppressorJustification);

	internal static readonly SuppressionDescriptor ParameterRule = new(
		id: "USP0025",
		suppressedDiagnosticId: "IDE0060",
		justification: Strings.InputSystemMessageSuppressorJustification);

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(
		MethodRule,
		ParameterRule
	);

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (var diagnostic in context.ReportedDiagnostics)
		{
			AnalyzeDiagnostic(diagnostic, context);
		}
	}

	private void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
	{
		var node = context.GetSuppressibleNode<SyntaxNode>(diagnostic, n => n is ParameterSyntax or MethodDeclarationSyntax);

		if (node is ParameterSyntax)
		{
			node = node
				.Ancestors()
				.OfType<MethodDeclarationSyntax>()
				.FirstOrDefault();
		}

		if (node == null)
			return;

		if (diagnostic.Location.SourceTree is not { } syntaxTree)
			return;

		var model = context.GetSemanticModel(syntaxTree);
		if (model.GetDeclaredSymbol(node) is not IMethodSymbol methodSymbol)
			return;

		if (!IsInputSystemMessage(methodSymbol))
			return;

		foreach (var suppression in SupportedSuppressions)
		{
			if (suppression.SuppressedDiagnosticId == diagnostic.Id)
				context.ReportSuppression(Suppression.Create(suppression, diagnostic));
		}
	}

	private static bool IsInputSystemMessage(IMethodSymbol methodSymbol)
	{
		// With the SendMessages/BroadcastMessages notification behavior, PlayerInput invokes
		// 'On' + action name methods taking an optional InputValue argument, and device/player
		// notifications (OnDeviceLost, OnPlayerJoined, ...) taking a PlayerInput argument.
		// With the UnityEvents notification behavior, callbacks take an InputAction.CallbackContext
		// argument and are invoked through serialized events.
		if (!methodSymbol.ReturnsVoid)
			return false;

		if (!methodSymbol.Name.StartsWith("On", StringComparison.Ordinal))
			return false;

		if (!methodSymbol.ContainingType.Extends(typeof(UnityEngine.MonoBehaviour)))
			return false;

		if (methodSymbol.Parameters.Length != 1)
			return false;

		var parameterType = methodSymbol.Parameters[0].Type;
		return parameterType.Matches(typeof(UnityEngine.InputSystem.InputValue))
			   || parameterType.Matches(typeof(UnityEngine.InputSystem.InputAction.CallbackContext))
			   || parameterType.Matches(typeof(UnityEngine.InputSystem.PlayerInput));
	}
}
