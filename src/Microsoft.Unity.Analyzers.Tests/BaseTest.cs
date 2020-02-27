/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public abstract class BaseTestDiagnosticVerifier<TAnalyzer> : DiagnosticVerifier
		where TAnalyzer : DiagnosticAnalyzer, new() 
	{
		[Fact]
		public void DoNotFailWithInterfaceMembers()
		{
			const string test = @"
using UnityEngine;

interface IFailure
{
	void FixedUpdate();
}
";
			
			VerifyCSharpDiagnostic(test);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TAnalyzer();
		}
	}

	public abstract class BaseTestSuppressorVerifier<TAnalyzer> : SuppressorVerifier
		where TAnalyzer : DiagnosticSuppressor, new() 
	{
		[Fact]
		public void DoNotFailWithInterfaceMembers()
		{
			const string test = @"
using UnityEngine;

interface IFailure
{
	void FixedUpdate();
}
";
			
			VerifyCSharpDiagnostic(test);
		}

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new TAnalyzer();
		}
	}

	public abstract class BaseTestCodeFixVerifier<TAnalyzer, TCodeFix> : CodeFixVerifier
		where TAnalyzer : DiagnosticAnalyzer, new() 
		where TCodeFix : CodeFixProvider, new() 
	{
		[Fact]
		public void DoNotFailWithInterfaceMembers()
		{
			const string test = @"
using UnityEngine;

interface IFailure
{
	void FixedUpdate();
}
";
			
			VerifyCSharpDiagnostic(test);
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
