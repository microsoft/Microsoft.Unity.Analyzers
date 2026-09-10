/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class UnusedMethodSuppressorTests : BaseSuppressorVerifierTest<UnusedMethodSuppressor>
{
	[Fact]
	public async Task UnusedMethodSuppressed()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Start()
    {
        Invoke(""InvokeMe"", 10.0f);
    }

    private void InvokeMe()
    {
        Start(); // we do not want to check for IDE0051 for Unity messages.
    }
}";

		var suppressor = ExpectSuppressor(UnusedMethodSuppressor.Rule)
			.WithLocation(11, 18);

		await VerifyCSharpDiagnosticAsync(test, suppressor);
	}

	[Fact]
	public async Task UnusedMethodIsInvokingSuppressed()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Start()
    {
        IsInvoking(""InvokeMe"");
    }

    private void InvokeMe()
    {
        Start();
    }
}";

		var suppressor = ExpectSuppressor(UnusedMethodSuppressor.Rule)
			.WithLocation(11, 18);

		await VerifyCSharpDiagnosticAsync(test, suppressor);
	}

	[Fact]
	public async Task UnusedMethodIsInvokingUserDefinedNotSuppressed()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Start()
    {
        IsInvoking(""InvokeMe"");
    }

    public new bool IsInvoking(string methodName)
    {
        Start();
        return false;
    }

    private void InvokeMe()
    {
    }
}";

		var diagnostic = ExpectNotSuppressed(UnusedMethodSuppressor.Rule)
			.WithSeverity(DiagnosticSeverity.Info)
			.WithMessage("Private member 'Camera.InvokeMe' is unused")
			.WithLocation(17, 18);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
	}

	[Fact]
	public async Task UnusedMethodMixedTypes()
	{
		const string test = @"
using UnityEngine;

class A : MonoBehaviour
{
    private B b = null;

    public void Update()
    {
        b.InvokeRepeating(""Foo"", 1.0f, 1.0f);
    }

    class B : MonoBehaviour
    {
        void Foo()
        {
        }
    }
}";

		var suppressor = ExpectSuppressor(UnusedMethodSuppressor.Rule)
			.WithLocation(15, 14);

		await VerifyCSharpDiagnosticAsync(test, suppressor);
	}
}
