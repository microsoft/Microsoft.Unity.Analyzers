/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class RequireComponentTests : BaseCodeFixVerifierTest<RequireComponentAnalyzer, RequireComponentCodeFix>
{
	[Fact]
	public async Task GetComponent()
	{
		const string test = @"
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    void Start()
    {
        var rb = GetComponent<Rigidbody>();
    }
}";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 18);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerScript : MonoBehaviour
{
    void Start()
    {
        var rb = GetComponent<Rigidbody>();
    }
}";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task ThisGetComponent()
	{
		const string test = @"
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    void Start()
    {
        var rb = this.GetComponent<Rigidbody>();
    }
}";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 18);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerScript : MonoBehaviour
{
    void Start()
    {
        var rb = this.GetComponent<Rigidbody>();
    }
}";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task MonoBehaviourInstance()
	{
		const string test = @"
using UnityEngine;

public class FooComponent : Component
{
    void Foo()
    {
        var rb = this.GetComponent<Rigidbody>();
    }
}";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task ThisType()
	{
		const string test = @"
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    void Start()
    {
		var foo = new GameObject();
        var rb = foo.GetComponent<Rigidbody>();
    }
}";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task AlreadyRequired()
	{
		const string test = @"
using UnityEngine;

[RequireComponent(typeof(object))]
[RequireComponent(typeof(object), typeof(Rigidbody))]
public class PlayerScript : MonoBehaviour
{
    void Start()
    {
        var rb = GetComponent<Rigidbody>();
    }
}";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task InvocationNullChecked()
	{
		const string test = @"
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    void Start()
    {
        if (GetComponent<Rigidbody>() == null) {
        }
    }
}";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task InvocationNotNullChecked()
	{
		const string test = @"
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    void Start()
    {
        if (null != GetComponent<Rigidbody>()) {
        }
    }
}";

		await VerifyCSharpDiagnosticAsync(test);
	}
}
