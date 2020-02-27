/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class EmptyUnityMessageTests : BaseCodeFixVerifierTest<EmptyUnityMessageAnalyzer, EmptyUnityMessageCodeFix>
	{
		[Fact]
		public void EmptyFixedUpdate()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void FixedUpdate()
    {
    }

    private void Foo()
    {
    }
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(6, 18)
				.WithArguments("FixedUpdate");

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{

    private void Foo()
    {
    }
}
";
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void FixedUpdateWithBody()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private void FixedUpdate()
    {
        Debug.Log(nameof(FixedUpdate));
    }

    private void Foo()
    {
    }
}
";
			VerifyCSharpDiagnostic(test);
		}

		[Fact]
		public void VirtualFixedUpdate()
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
			VerifyCSharpDiagnostic(test);
		}

		[Fact]
		public void VirtualOverrideFixedUpdate()
		{
			const string test = @"
using UnityEngine;

class BaseBehaviour : MonoBehaviour
{
	public virtual void FixedUpdate()
	{
	}
}

class Camera : BaseBehaviour
{
    public override void FixedUpdate()
    {
    }
}
";
			VerifyCSharpDiagnostic(test);
		}

		[Fact]
		public void AbstractOverrideFixedUpdate()
		{
			const string test = @"
using UnityEngine;

abstract class BaseBehaviour : MonoBehaviour
{
	public abstract void FixedUpdate();
}

class Camera : BaseBehaviour
{
    public override void FixedUpdate()
    {
    }
}
";
			VerifyCSharpDiagnostic(test);
		}
	}
}
