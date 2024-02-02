/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class GetLocalPositionAndRotationTests : BaseCodeFixVerifierTest<GetLocalPositionAndRotationAnalyzer, GetLocalPositionAndRotationCodeFix>
{
	// For extensive testing, see GetPositionAndRotationTests
	[SkippableFact]
	public async Task UseGetLocalPositionAndRotationMethod()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Quaternion rotation;
        rotation = transform.localRotation;
        var position = transform.localPosition;
    }
}
";

		var method = GetCSharpDiagnosticAnalyzer().ExpressionContext.PositionAndRotationMethodName;
		var type = typeof(UnityEngine.Transform);

		Skip.IfNot(MethodExists("UnityEngine", type.FullName!, method), $"This Unity version does not support {type}.{method}");

		var diagnostic = ExpectDiagnostic().WithLocation(9, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        Quaternion rotation;
        transform.GetLocalPositionAndRotation(out var position, out rotation);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[SkippableFact]
	public async Task UseGetLocalPositionAndRotationMethodTransformAccess()
	{
		const string test = @"
using UnityEngine.Jobs;

class Context
{
    void Method()
    {
        var stub = new TransformAccess();
        var foo = stub.localPosition;
        var bar = stub.localRotation;
    }
}
";

		var method = GetCSharpDiagnosticAnalyzer().ExpressionContext.PositionAndRotationMethodName;
		var type = typeof(UnityEngine.Jobs.TransformAccess);

		Skip.IfNot(MethodExists("UnityEngine", type.FullName!, method), $"This Unity version does not support {type}.{method}");

		var diagnostic = ExpectDiagnostic().WithLocation(9, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine.Jobs;

class Context
{
    void Method()
    {
        var stub = new TransformAccess();
        stub.GetLocalPositionAndRotation(out var foo, out var bar);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[SkippableFact]
	public async Task NoTypeMismatch()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.Jobs;

class Camera : MonoBehaviour
{
    void Update()
    {
        var stub = new TransformAccess();
        var foo = stub.localPosition;
        var bar = transform.localRotation;
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}
}
