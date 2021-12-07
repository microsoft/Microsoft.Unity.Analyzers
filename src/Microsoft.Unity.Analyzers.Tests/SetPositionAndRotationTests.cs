/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class SetPositionAndRotationTests : BaseCodeFixVerifierTest<SetPositionAndRotationAnalyzer, SetPositionAndRotationCodeFix>
{
	[Fact]
	public async Task UpdatePositionAndRotationMethod()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        transform.position = new Vector3(0.0f, 1.0f, 0.0f);
        transform.rotation = transform.rotation;
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(8, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        transform.SetPositionAndRotation(new Vector3(0.0f, 1.0f, 0.0f), transform.rotation);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task UpdatePositionAndRotationMethodComments()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        // position
        transform.position = /* inner position */ new Vector3(0.0f, 1.0f, 0.0f) /* outer position */;
        // rotation
        transform.rotation = /* inner rotation */ transform.rotation /* outer rotation */;
        // trailing
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(9, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        // position
        // rotation
        transform.SetPositionAndRotation(
/* inner position */ new Vector3(0.0f, 1.0f, 0.0f) /* outer position */,
/* inner rotation */ transform.rotation /* outer rotation */);
        // trailing
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task MultiplePositionChanges()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        transform.position = new Vector3(0.0f, 1.0f, 0.0f);
        transform.position = new Vector3(0.5f, 1.0f, 2.0f);
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task SeparateBlocks()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Start()
    {
        transform.position = new Vector3(0.0f, 1.0f, 0.0f);
    }

    void Update()
    {
        transform.rotation = transform.rotation;
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task PositionVariableUpdate()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Vector3 newPosition = new Vector3(1,2,3);
        transform.rotation = transform.rotation;
        transform.position = newPosition;
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(9, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Vector3 newPosition = new Vector3(1,2,3);
        transform.SetPositionAndRotation(newPosition, transform.rotation);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task MemberExpression()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        GameObject instance = null;
        GameObject go = null;
        instance.transform.position = go.transform.position;
        instance.transform.rotation = go.transform.rotation;
    }
}
";

		var diagnostic = ExpectDiagnostic().WithLocation(10, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        GameObject instance = null;
        GameObject go = null;
        instance.transform.SetPositionAndRotation(go.transform.position, go.transform.rotation);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task DistinctMemberExpression()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        GameObject instance = null;
        GameObject go = null;
        instance.transform.position = go.transform.position;
        go.transform.rotation = go.transform.rotation;
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}
}
