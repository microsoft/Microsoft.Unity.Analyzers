/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Unity.Analyzers;

[AttributeUsage(AttributeTargets.Method)]
public class MethodUsageAttribute : Attribute
{
}

public abstract class MethodUsageAnalyzer<T> : DiagnosticAnalyzer where T : MethodUsageAttribute
{
	private static ILookup<string, MethodInfo>? _lookup;

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	protected virtual bool IsReportable(IMethodSymbol method)
	{
		_lookup ??= CollectMethods()
			.Where(m => m.DeclaringType != null)
			.ToLookup(m => m.DeclaringType!.FullName);

		// lookup returns an empty collection for nonexistent keys
		var typename = method.ContainingType.ToDisplayString();
		return _lookup[typename].Any(method.Matches);
	}

	protected virtual IEnumerable<MethodInfo> CollectMethods()
	{
		return CollectMethods(GetType().Assembly);
	}

	protected static IEnumerable<MethodInfo> CollectMethods(params Type[] types)
	{
		return types
			.SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			.Where(m => m.GetCustomAttributes(typeof(T), true).Length > 0);
	}

	protected static IEnumerable<MethodInfo> CollectMethods(Assembly assembly)
	{
		return CollectMethods(assembly.GetTypes());
	}

	private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		var invocation = (InvocationExpressionSyntax)context.Node;

		if (invocation.Expression is not MemberAccessExpressionSyntax member)
			return;

		var symbol = context.SemanticModel.GetSymbolInfo(member);

		if (symbol.Symbol is not IMethodSymbol method)
			return;

		if (!IsReportable(method))
			return;

		context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics.First(), member.Name.GetLocation(), method.Name));
	}
}
