﻿/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<EmptyUnityMessageAnalyzer, EmptyUnityMessageCodeFix>;

	public class EmptyUnityMessageTests : BaseTest<EmptyUnityMessageAnalyzer, EmptyUnityMessageCodeFix>
	{
		[Fact]
		public async Task EmptyFixedUpdate()
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

			var diagnostic = Verify.Diagnostic().WithLocation(6, 18).WithArguments("FixedUpdate");

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{

    private void Foo()
    {
    }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task FixedUpdateWithBody()
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
			await Verify.VerifyAnalyzerAsync(test);
		}

		[Fact]
		public async Task VirtualFixedUpdate()
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
			await Verify.VerifyAnalyzerAsync(test);
		}

		[Fact]
		public async Task VirtualOverrideFixedUpdate()
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
			await Verify.VerifyAnalyzerAsync(test);
		}

		[Fact]
		public async Task AbstractOverrideFixedUpdate()
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
			await Verify.VerifyAnalyzerAsync(test);
		}
	}
}
