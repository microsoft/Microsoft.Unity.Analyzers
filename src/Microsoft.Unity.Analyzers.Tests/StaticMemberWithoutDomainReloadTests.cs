/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class StaticMemberWithoutDomainReloadTests : BaseDiagnosticVerifierTest<StaticMemberWithoutDomainReloadAnalyzer>
{
	private static AnalyzerVerificationContext Context => AnalyzerVerificationContext
													.Default
													.WithPreprocessorSymbol(StaticMemberWithoutDomainReloadAnalyzer.PreprocessorSymbolDisableDomainReload);


	[Fact]
	public async Task TestFieldAccess()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private static class StaticClass
    {
        public static int counter = 0;
    }

    private void Update()
	{
        StaticClass.counter = 66;
	}
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(13, 9);

		await VerifyCSharpDiagnosticAsync(test); // without proper define => no diagnostic
		await VerifyCSharpDiagnosticAsync(Context, test, diagnostic);
	}

	
	[Fact]
	public async Task TestFieldAssignment()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private static int counter = 0;

    private void Update()
    {
        counter = 66;
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10,9);

		await VerifyCSharpDiagnosticAsync(test); // without proper define => no diagnostic
		await VerifyCSharpDiagnosticAsync(Context, test, diagnostic);
	}
	
	[Fact]
	public async Task TestFieldIncrement()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private static int counter = 0;

    private void Update()
    {
        counter++;
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 9);

		await VerifyCSharpDiagnosticAsync(test); // without proper define => no diagnostic
		await VerifyCSharpDiagnosticAsync(Context, test, diagnostic);
	}

	[Fact]
	public async Task TestFieldDecrement()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private static int counter = 0;

    private void Update()
    {
        --counter;
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 11);

		await VerifyCSharpDiagnosticAsync(test); // without proper define => no diagnostic
		await VerifyCSharpDiagnosticAsync(Context, test, diagnostic);
	}

	[Fact]
	public async Task TestEventHandler()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
    {
        Application.quitting += delegate { };
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 9);

		await VerifyCSharpDiagnosticAsync(test); // without proper define => no diagnostic
		await VerifyCSharpDiagnosticAsync(Context, test, diagnostic);
	}

	[Fact]
	public async Task TestOwnEventHandler()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public static event System.Action ownEvent; 

    private void Start()
    {
        ownEvent += delegate { };
    }

    private void Update()
    {
        ownEvent?.Invoke();
    }
}
";
		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 9);

		await VerifyCSharpDiagnosticAsync(test); // without proper define => no diagnostic
		await VerifyCSharpDiagnosticAsync(Context, test, diagnostic);
	}	
	
}
