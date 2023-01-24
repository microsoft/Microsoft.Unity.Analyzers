/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using Xunit.Sdk;

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods",
	Justification = "Tests",
	Scope = "namespaceanddescendants",
	Target = "~N:Microsoft.Unity.Analyzers.Tests")]

namespace Microsoft.Unity.Analyzers.Tests;

public abstract class BaseDiagnosticVerifierTest<TAnalyzer> : DiagnosticVerifier
	where TAnalyzer : DiagnosticAnalyzer, new()
{
	internal const string InterfaceTest = @"
using UnityEngine;

interface IFailure
{
	void FixedUpdate();
}

class Failure : MonoBehaviour, IFailure {

    void IFailure.FixedUpdate() {
    }
}
";

	[Fact]
	public async Task DoNotFailWithInterfaceMembers()
	{
		await VerifyCSharpDiagnosticAsync(InterfaceTest);
	}

	protected override TAnalyzer GetCSharpDiagnosticAnalyzer()
	{
		return new TAnalyzer();
	}
}

public abstract class BaseSuppressorVerifierTest<TAnalyzer> : SuppressorVerifier
	where TAnalyzer : DiagnosticSuppressor, new()
{
	[Fact]
	public async Task DoNotFailWithInterfaceMembers()
	{
		await VerifyCSharpDiagnosticAsync(BaseDiagnosticVerifierTest<TAnalyzer>.InterfaceTest);
	}

	protected override TAnalyzer GetCSharpDiagnosticAnalyzer()
	{
		return new TAnalyzer();
	}
}

public abstract class BaseCodeFixVerifierTest<TAnalyzer, TCodeFix> : CodeFixVerifier
	where TAnalyzer : DiagnosticAnalyzer, new()
	where TCodeFix : CodeFixProvider, new()
{
	[Fact]
	public async Task DoNotFailWithInterfaceMembers()
	{
		await VerifyCSharpDiagnosticAsync(BaseDiagnosticVerifierTest<TAnalyzer>.InterfaceTest);
	}

	protected override TAnalyzer GetCSharpDiagnosticAnalyzer()
	{
		return new TAnalyzer();
	}

	protected override TCodeFix GetCSharpCodeFixProvider()
	{
		return new TCodeFix();
	}

	protected bool MethodExists(string typeName, string methodName, string assenblyNameFilter = "")
	{
		foreach (var assemblyFile in UnityAssemblies().Where(n => n.Contains(assenblyNameFilter)))
		{
			var assembly = Assembly.LoadFile(assemblyFile);
			var type = assembly.GetType(typeName, false);
			if (type == null)
				continue;

			var method = type.GetMethod(methodName);
			if (method == null)
				continue;

			return true;
		}

		return false;
	}
}
