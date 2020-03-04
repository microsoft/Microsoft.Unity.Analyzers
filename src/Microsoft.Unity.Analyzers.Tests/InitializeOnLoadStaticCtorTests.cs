/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class InitializeOnLoadStaticCtorTests : BaseCodeFixVerifierTest<InitializeOnLoadStaticCtorAnalyzer, InitializeOnLoadStaticCtorCodeFix>
	{
		[Fact]
		public void InitializeOnLoadWithoutStaticCtor()
		{
			const string test = @"
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
class Camera : MonoBehaviour
{
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(6, 7)
				.WithArguments("Camera");

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
class Camera : MonoBehaviour
{
    static Camera()
    {
    }
}
";
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void InitializeOnLoadWithImplicitStaticCtor()
		{
			const string test = @"
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public sealed class Camera : MonoBehaviour
{
    public static readonly int willGenerateImplicitStaticCtor = 666;
}
";

			var diagnostic = ExpectDiagnostic()
				.WithLocation(6, 21)
				.WithArguments("Camera");

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public sealed class Camera : MonoBehaviour
{
    static Camera()
    {
    }

    public static readonly int willGenerateImplicitStaticCtor = 666;
}
";
			VerifyCSharpFix(test, fixedTest);
		}

	}
}
