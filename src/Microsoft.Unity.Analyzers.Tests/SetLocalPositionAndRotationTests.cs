/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class SetLocalPositionAndRotationTests : BaseCodeFixVerifierTest<SetLocalPositionAndRotationAnalyzer, SetLocalPositionAndRotationCodeFix>
{
	// For extensive testing, see SetPositionAndRotationTests.cs
	[SkippableFact]
	public async Task UpdateLocalPositionAndRotationMethod()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        transform.localPosition = new Vector3(0.0f, 1.0f, 0.0f);
        transform.localRotation = transform.localRotation;
    }
}
";

		var method = GetCSharpDiagnosticAnalyzer().ExpressionContext.PositionAndRotationMethodName;
		var type = typeof(UnityEngine.Transform);

		Skip.IfNot(MethodExists("UnityEngine", type.FullName!, method), $"This Unity version does not support {type}.{method}");

		var diagnostic = ExpectDiagnostic().WithLocation(8, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        transform.SetLocalPositionAndRotation(new Vector3(0.0f, 1.0f, 0.0f), transform.localRotation);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[SkippableFact]
	public async Task UpdateLocalPositionAndRotationMethodTransformAccess()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.Jobs;

class Context
{
    void Method()
    {
        var stub = new TransformAccess();
        stub.localPosition = new Vector3(0.0f, 1.0f, 0.0f);
        stub.localRotation = stub.localRotation;
    }
}
";

		var method = GetCSharpDiagnosticAnalyzer().ExpressionContext.PositionAndRotationMethodName;
		var type = typeof(UnityEngine.Jobs.TransformAccess);

		Skip.IfNot(MethodExists("UnityEngine", type.FullName!, method), $"This Unity version does not support {type}.{method}");

		var diagnostic = ExpectDiagnostic().WithLocation(10, 9);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;
using UnityEngine.Jobs;

class Context
{
    void Method()
    {
        var stub = new TransformAccess();
        stub.SetLocalPositionAndRotation(new Vector3(0.0f, 1.0f, 0.0f), stub.localRotation);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[SkippableFact]
	public async Task UpdateLocalPositionAndRotationMethodTrivia()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Update()
    {
        // leading comment
        transform.localPosition = new Vector3(0.0f, 1.0f, 0.0f);
        transform.localRotation = transform.localRotation;
        // trailing comment
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
        // leading comment
        transform.SetLocalPositionAndRotation(new Vector3(0.0f, 1.0f, 0.0f), transform.localRotation);
        // trailing comment
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
