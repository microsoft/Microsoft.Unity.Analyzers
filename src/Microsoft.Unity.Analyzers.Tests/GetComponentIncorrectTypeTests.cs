/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
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
		public async Task GetComponentLegacyTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
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
		public async Task GetGenericMethodComponentIncorrectTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Method<T>()
    {
        GetComponent<T>();
    }
}
";
			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 9)
				.WithArguments("T");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);
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
		public async Task GetGenericClassComponentIncorrectTest()
		{
			const string test = @"
using UnityEngine;

class Camera<T> : MonoBehaviour
{
    private void Method()
    {
        GetComponent<T>();
    }
}
";
			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 9)
				.WithArguments("T");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);
		}
	}
}
