/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class MeshPropertyElementCounterTests : BaseCodeFixVerifierTest<MeshPropertyElementCounterAnalyzer, MeshPropertyElementCounterCodeFix>
{
	[Fact]
	public async Task ByLengthProperty()
	{
		const string test = @"
class Dummy {
	public void Test(UnityEngine.Mesh m) {
        var count = m.vertices.Length;
    }
}
";

		const string fixedSource = @"
class Dummy {
	public void Test(UnityEngine.Mesh m) {
        var count = m.vertexCount;
    }
}
";

		var diagnostic = ExpectDiagnostic(MeshPropertyElementCounterAnalyzer.Rule)
			.WithLocation(4, 21)
			.WithArguments("vertexCount", "vertices");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
		await VerifyCSharpFixAsync(test, fixedSource);
	}

	[Fact]
	public async Task ByCountMethod()
	{
		const string test = @"
using System.Linq;

class Dummy {
	public void Test(UnityEngine.Mesh m) {
        var count = m.vertices.Count(); // TODO: why CS1061 on local?
    }
}
";

		const string fixedSource = @"
using System.Linq;

class Dummy {
	public void Test(UnityEngine.Mesh m) {
        var count = m.vertexCount;
    }
}
";
		var diagnostic = ExpectDiagnostic(MeshPropertyElementCounterAnalyzer.Rule)
			.WithLocation(4, 21)
			.WithArguments("vertexCount", "vertices");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
		await VerifyCSharpFixAsync(test, fixedSource);
	}

	[Fact]
	public async Task ByDesugaredCountMethod()
	{
		const string test = @"
class Dummy {
	public void Test(UnityEngine.Mesh m) {
        var count = System.Linq.Enumerable.Count(m.vertices); // TODO: why CS1069 on local?
    }
}
";

		const string fixedSource = @"
class Dummy {
	public void Test(UnityEngine.Mesh m) {
        var count = m.vertexCount;
    }
}
";
		var diagnostic = ExpectDiagnostic(MeshPropertyElementCounterAnalyzer.Rule)
			.WithLocation(4, 21)
			.WithArguments("vertexCount", "vertices");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
		await VerifyCSharpFixAsync(test, fixedSource);
	}
}
