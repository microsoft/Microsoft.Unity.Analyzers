/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class GetPositionAndRotationTests : BaseCodeFixVerifierTest<GetPositionAndRotationAnalyzer, GetPositionAndRotationCodeFix>
{
	[Fact]
	public async Task UseGetPositionAndRotationMethod()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Vector3 position;
        Quaternion rotation;
        position = transform.position;
        rotation = transform.rotation;
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
        Vector3 position;
        Quaternion rotation;
        transform.GetPositionAndRotation(out position, out rotation);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[SkippableFact]
	public async Task UseGetPositionAndRotationMethodTransformAccess()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.Jobs;

class Context
{
    void Method()
    {
        Vector3 position;
        Quaternion rotation;
        var stub = new TransformAccess();
        position = stub.position;
        rotation = stub.rotation;
    }
}
";

		var method = GetCSharpDiagnosticAnalyzer().ExpressionContext.PositionAndRotationMethodName;
		var type = typeof(UnityEngine.Jobs.TransformAccess);

		Skip.IfNot(MethodExists("UnityEngine", type.FullName!, method), $"This Unity version does not support {type}.{method}");

		var diagnostic = ExpectDiagnostic().WithLocation(12, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;
using UnityEngine.Jobs;

class Context
{
    void Method()
    {
        Vector3 position;
        Quaternion rotation;
        var stub = new TransformAccess();
        stub.GetPositionAndRotation(out position, out rotation);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task OutRefCompatibility()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        transform.position = transform.position;
        transform.rotation = transform.rotation;
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task ImplicitVariableDeclaration()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        var position = transform.position;
        var rotation = transform.rotation;
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
        transform.GetPositionAndRotation(out var position, out var rotation);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task ExplicitVariableDeclaration()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
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
        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}


}
