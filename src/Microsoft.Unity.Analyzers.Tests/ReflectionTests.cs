/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class ReflectionTests : BaseDiagnosticVerifierTest<ReflectionAnalyzer>
	{
		[Fact]
		public async Task ReflectionInUpdateTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Update()
    {
        GetType().GetMethod(""Update"");
    }
}
";
			var diagnostic = ExpectDiagnostic()
				.WithLocation(8, 9)
				.WithArguments("Update");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);
		}

		[Fact]
		public async Task ReflectionInAwakeTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Awake()
    {
        GetType().GetMethod(""Update"");
    }
}
";

			await VerifyCSharpDiagnosticAsync(test);
		}

		[Fact]
		public async Task ReflectionInNonMessageMethodTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public void Foo()
    {
        GetType().GetMethod(""Update"");
    }
}
";

			await VerifyCSharpDiagnosticAsync(test);
		}

		[Fact]
		public async Task ReflectionInNonMessageTypeTest()
		{
			const string test = @"
class Bar 
{
    public void Update()
    {
        GetType().GetMethod(""Update"");
    }
}
";

			await VerifyCSharpDiagnosticAsync(test);
		}

		[Fact]
		public async Task AbstractUpdateTest()
		{
			const string test = @"
using UnityEngine;

abstract class Camera : MonoBehaviour
{
    public abstract void Update();
}
";

			await VerifyCSharpDiagnosticAsync(test);
		}



	}
}
