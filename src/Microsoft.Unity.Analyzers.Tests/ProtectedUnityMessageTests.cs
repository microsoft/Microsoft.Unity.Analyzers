/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class ProtectedUnityMessageTests : BaseCodeFixVerifierTest<ProtectedUnityMessageAnalyzer, ProtectedUnityMessageCodeFix>
	{
		[Fact]
		public async Task FixPrivateUnityMessage()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void Start()
	{
	}

    private void Foo()
	{
	}
}
";

			var diagnostic = ExpectDiagnostic().WithLocation(6,18);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    protected void Start()
	{
	}

    private void Foo()
	{
	}
}
";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task FixPublicWithModifiersUnityMessage()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public virtual void Awake()
	{
	}

    protected void Update()
	{
	}
}
";

			var diagnostic = ExpectDiagnostic().WithLocation(6,25);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    protected virtual void Awake()
	{
	}

    protected void Update()
	{
	}
}
";

			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task FixModifiersUnityMessage()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public virtual void FixedUpdate()
	{
	}
}
";

			var diagnostic = ExpectDiagnostic().WithLocation(6, 25);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    protected virtual void FixedUpdate()
	{
	}
}
";

			await VerifyCSharpFixAsync(test, fixedTest);
		}
	}
}
