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

	[Fact]
	public async Task VariableDeclarationNotNullConditionTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() 
    {
        var rb = gameObject.GetComponent<Rigidbody>();
        if (rb != null) {
            Debug.Log(rb.name);
        }
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task VariableDeclarationNotNullConditionReverseOperandsTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() 
    {
        var rb = gameObject.GetComponent<Rigidbody>();
        if (null != rb) {
            Debug.Log(rb.name);
        }
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task VariableDeclarationNullConditionTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() 
    {
        var rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) {
            Debug.Log(""null!"");
        }
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task PropertyAssignmentNotNullConditionTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private Rigidbody rb { get; set;}

    public void Update() 
    {
        rb = gameObject.GetComponent<Rigidbody>();
        if (rb != null) {
            Debug.Log(rb.name);
        }
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task NoGetComponentDerivatives()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update() 
    {
        var rb = gameObject.GetComponentInChildren<Rigidbody>();
        if (rb != null) {
            Debug.Log(rb.name);
        }
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task GetComponentTrivia()
	{
		const string test = @"
using UnityEngine;

// class comment
public class PlayerScript : MonoBehaviour
{
    void Start()
    {
        var rb = GetComponent<Rigidbody>(); // trailing comment
    }
}";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(9, 18);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

// class comment
[RequireComponent(typeof(Rigidbody))]
public class PlayerScript : MonoBehaviour
{
    void Start()
    {
        var rb = GetComponent<Rigidbody>(); // trailing comment
    }
}";

		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
