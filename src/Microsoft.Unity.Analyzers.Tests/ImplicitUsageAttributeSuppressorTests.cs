/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class ImplicitUsageAttributeSuppressorTests : BaseSuppressorVerifierTest<ImplicitUsageAttributeSuppressor>
{
	[Fact]
	public async Task UnityPreserveTest()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.Scripting;

class Camera : MonoBehaviour
{
    [Preserve]
    private void Foo() {
    }
}
";

		var suppressor = ExpectSuppressor(ImplicitUsageAttributeSuppressor.Rule)
			.WithLocation(8, 18);

		await VerifyCSharpDiagnosticAsync(test, suppressor);
	}

	[Fact]
	public async Task OwnPreserveTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [My.Own.Stuff.Preserve]
    private void Foo() {
    }
}

namespace My.Own.Stuff {
    public class PreserveAttribute : System.Attribute { }
}
";

		var suppressor = ExpectSuppressor(ImplicitUsageAttributeSuppressor.Rule)
			.WithLocation(7, 18);

		await VerifyCSharpDiagnosticAsync(test, suppressor);
	}

	[Fact]
	public async Task UsedImplicitlyTest()
	{
		const string test = @"
using UnityEngine;
using JetBrains.Annotations;

class Camera : MonoBehaviour
{
    [UsedImplicitly]
    private void Foo() {
    }
}
";

		var suppressor = ExpectSuppressor(ImplicitUsageAttributeSuppressor.Rule)
			.WithLocation(8, 18);

		await VerifyCSharpDiagnosticAsync(test, suppressor);
	}
}
