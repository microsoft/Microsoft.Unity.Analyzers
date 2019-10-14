using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public abstract class BaseTest<TAnalyzer, TCodeFix>
		where TAnalyzer : DiagnosticAnalyzer, new() 
		where TCodeFix : CodeFixProvider, new()
	{
		[Fact]
		public async Task DoNotFailWithInterfaceMembers()
		{
			const string test = @"
using UnityEngine;

interface IFailure
{
	void FixedUpdate();
}
";

			await UnityCodeFixVerifier<TAnalyzer, TCodeFix>.VerifyAnalyzerAsync(test);
		}
	}
}
