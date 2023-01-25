/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

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

	protected bool MethodExists(string assemblyName, string typeName, string methodName)
	{
		var unityAssemblies = UnityAssemblies().ToArray();
		var assemblyFilePath = unityAssemblies.First(f => Path.GetFileNameWithoutExtension(f) == assemblyName);

		var resolver = new PathAssemblyResolver(unityAssemblies);
		var ctx = new MetadataLoadContext(resolver);

		var assembly = ctx.LoadFromAssemblyPath(assemblyFilePath);
		Assert.NotNull(assembly);

		var type = assembly.GetType(typeName, throwOnError: false);
		if (type == null)
			return false;

		var method = type.GetMethod(methodName);
		return method != null;
	}
}
