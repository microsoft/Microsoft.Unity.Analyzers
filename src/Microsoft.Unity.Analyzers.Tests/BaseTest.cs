/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public abstract class BaseDiagnosticVerifierTest<TAnalyzer> : DiagnosticVerifier
		where TAnalyzer : DiagnosticAnalyzer, new() 
	{

		internal const string InterfaceTest = @"
interface IFailure
{
	void FixedUpdate();
}
";

		[Fact]
		public async Task DoNotFailWithInterfaceMembersAsync()
		{
			await VerifyCSharpDiagnosticAsync(InterfaceTest);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TAnalyzer();
		}
	}

	public abstract class BaseSuppressorVerifierTest<TAnalyzer> : SuppressorVerifier
		where TAnalyzer : DiagnosticSuppressor, new() 
	{
		[Fact]
		public async Task DoNotFailWithInterfaceMembersAsync()
		{
			await VerifyCSharpDiagnosticAsync(BaseDiagnosticVerifierTest<TAnalyzer>.InterfaceTest);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TAnalyzer();
		}
	}

	public abstract class BaseCodeFixVerifierTest<TAnalyzer, TCodeFix> : CodeFixVerifier
		where TAnalyzer : DiagnosticAnalyzer, new() 
		where TCodeFix : CodeFixProvider, new() 
	{
		[Fact]
		public async Task DoNotFailWithInterfaceMembersAsync()
		{
			await VerifyCSharpDiagnosticAsync(BaseDiagnosticVerifierTest<TAnalyzer>.InterfaceTest);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TAnalyzer();
		}

		protected override CodeFixProvider GetCSharpCodeFixProvider()
		{
			return new TCodeFix();
		}
	}

}
