/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class GetComponentIncorrectTypeTests : BaseDiagnosticVerifierTest<GetComponentIncorrectTypeAnalyzer>
{
	[Fact]
	public async Task GetComponentInterfaceTypeTest()
	{
		const string test = @"
using System;
using UnityEngine;

class Camera : MonoBehaviour
{
    private IDisposable disp;

    private void Start()
    {
        disp = GetComponent<IDisposable>();
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task GetComponentCorrectTypeTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task GetComponentIncorrectTypeTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private int i;

    private void Start()
    {
        i = GetComponent<int>();
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 13)
			.WithArguments("Int32");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task TryGetComponentIncorrectTypeTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        if (!TryGetComponent<int>(out var i))
            return;
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 14)
			.WithArguments("Int32");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task GetComponentLegacyInconclusiveTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        // we only check for the generic overload.
        var hello = GetComponent(""Hello"");
    }
}
";
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task GetGenericMethodComponentCorrectTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Method<T>() where T : Component
    {
        var hello = GetComponent<T>();
    }
}
";
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task GetGenericClassComponentCorrectTest()
	{
		const string test = @"
using UnityEngine;

class Camera<T> : MonoBehaviour where T : Component
{
    private void Method()
    {
        var hello = GetComponent<T>();
    }
}
";
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task GetGenericClassComponentInconclusiveTest()
	{
		const string test = @"
using UnityEngine;

class Camera<T> : MonoBehaviour
{
    private void Method()
    {
        // We need to infer on usages to be able to support this. For now, we don't.
        GetComponent<T>();
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task GetGenericMethodInconclusiveTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Method<T>()
    {
        // We need to infer on usages to be able to support this. For now, we don't.
        GetComponent<T>();
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task GetGenericMethodExplicitInterfaceCorrectTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        Test<IMyInterface>();
    }

    private void Test<T>() where T : IMyInterface
    {
        gameObject.GetComponent<T>();
    }

    private interface IMyInterface { }
}
";
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task GetGenericMethodExplicitTypeCorrectTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        Test<Component>();
    }

    private void Test<T>() where T : Component
    {
        gameObject.GetComponent<T>();
    }
}
";
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task GetGenericMethodExplicitTypeIncorrectTest()
	{
		const string test = @"
using System;
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        Test<Exception>();
    }

    private void Test<T>() where T : Exception
    {
        gameObject.GetComponent<T>();
    }
}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(14, 9)
			.WithArguments("T");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task GetGenericMethodTypeReferenceConstraintInconclusiveTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        // this one is valid, 
        Test<IMyInterface>();

        // this one is not, but we need to infer on usages to be able to support this. For now, we don't.
        Test<object>();
    }

    private void Test<T>() where T : class
    {
        gameObject.GetComponent<T>();
    }

    private interface IMyInterface { }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}
}
