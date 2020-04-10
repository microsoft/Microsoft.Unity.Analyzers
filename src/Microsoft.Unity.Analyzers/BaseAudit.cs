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

namespace Microsoft.Unity.Analyzers
{
	[AttributeUsage(AttributeTargets.Method)]
	public class AuditAttribute : Attribute
	{
	}

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public abstract class BaseAuditAnalyzer<T> : DiagnosticAnalyzer where T : AuditAttribute
	{
		private static ILookup<string, MethodInfo> _lookup;

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
		}

		protected virtual bool IsReportable(IMethodSymbol method)
		{
			if (_lookup == null)
			{
				_lookup = CollectMethods(GetType().Assembly)
					.Where(m => m.DeclaringType != null)
					.ToLookup(m => m.DeclaringType.FullName);
			}

			// lookup returns an empty collection for nonexistent keys
			var typename = method.ContainingType.ToDisplayString();
			return _lookup[typename].Any(method.Matches);
		}

		private static IEnumerable<MethodInfo> CollectMethods(Assembly assembly)
		{
			return assembly
				.GetTypes()
				.SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
				.Where(m => m.GetCustomAttributes(typeof(T), true).Length > 0);
		}

		private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
		{
			var invocation = (InvocationExpressionSyntax)context.Node;
			var symbol = context.SemanticModel.GetSymbolInfo(invocation);
			if (symbol.Symbol == null)
				return;

			if (!(symbol.Symbol is IMethodSymbol method))
				return;

			if (!IsReportable(method))
				return;

			context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics.First(), invocation.GetLocation(), method.Name));
		}
	}
}
