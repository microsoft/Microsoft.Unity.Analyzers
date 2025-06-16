/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class MeshPropertyAllocationTests : BaseCodeFixVerifierTest<MeshPropertyAllocationAnalyzer, MeshPropertyAllocationCodeFix>
{
	[Fact]
	public async Task ForStatement()
	{
		const string test = @"
class DummyClass
{
    private void Test(UnityEngine.Mesh mesh) {
        for (var i = 0; i <= 5; i++) {
            var c = mesh.uv;
        }
    }
}
";

		var diagnostic = ExpectDiagnostic(MeshPropertyAllocationAnalyzer.Rule)
			.WithLocation(6, 21)
			.WithArguments("uv");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task ProvePropertyExhaustiveness()
	{
		string[] propertyNames = ["uv", "uv2", "uv3", "uv4", "uv5", "uv6", "uv7", "vertices", "colors", "colors32"];
		foreach (var name in propertyNames)
		{
			var diagnostic = ExpectDiagnostic(MeshPropertyAllocationAnalyzer.Rule)
				.WithLocation(6, 21)
				.WithArguments(name);

			await VerifyCSharpDiagnosticAsync(CreateSnippet(name), diagnostic);
		}

		return;

		static string CreateSnippet(string n) =>
			$$"""

			  class DummyClass
			  {
			      private void Test(UnityEngine.Mesh mesh) {
			          for (var i = 0; i <= 5; i++) {
			              var c = mesh.{{n}};
			          }
			      }
			  }

			  """;
	}

	[Fact]
	public async Task ConditionInForStatement()
	{
		const string test = """

		                    using System.Collections.Generic;

		                    class DummyClass
		                    {
		                        private void Test(UnityEngine.Mesh mesh) {
		                            var accumulator = new List<int>();
		                            for (var i = 0; i < mesh.uv.Length; i++) {
		                                accumulator[i] = i * 2;
		                            }
		                        }
		                    }

		                    """;

		var diagnostic = ExpectDiagnostic(MeshPropertyAllocationAnalyzer.Rule)
			.WithLocation(8, 29)
			.WithArguments("uv");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task WhileStatement()
	{
		const string test = """

		                    class DummyClass
		                    {
		                        private void Test(UnityEngine.Mesh mesh) {
		                            while (true) {
		                                var c = mesh.uv;
		                            }
		                        }
		                    }

		                    """;

		var diagnostic = ExpectDiagnostic(MeshPropertyAllocationAnalyzer.Rule)
			.WithLocation(6, 21)
			.WithArguments("uv");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task DoWhileStatement()
	{
		const string test = """

		                    class DummyClass
		                    {
		                        private void Test(UnityEngine.Mesh mesh) {
		                            do {
		                                var c = mesh.uv;
		                            } while (true);
		                        }
		                    }

		                    """;

		var diagnostic = ExpectDiagnostic(MeshPropertyAllocationAnalyzer.Rule)
			.WithLocation(6, 21)
			.WithArguments("uv");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task ForEachStatement()
	{
		const string test = """

		                    using System.Collections.Generic;

		                    class DummyClass
		                    {
		                        private void Test(UnityEngine.Mesh mesh, IEnumerable<int> ints) {
		                            foreach (var i in ints) {
		                                var c = mesh.uv;
		                            }
		                        }
		                    }

		                    """;

		var diagnostic = ExpectDiagnostic(MeshPropertyAllocationAnalyzer.Rule)
			.WithLocation(8, 21)
			.WithArguments("uv");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}
}
