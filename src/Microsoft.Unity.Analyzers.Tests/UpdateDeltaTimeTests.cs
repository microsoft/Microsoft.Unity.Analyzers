/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class UpdateWithoutDeltaTimeTests : BaseCodeFixVerifierTest<UpdateDeltaTimeAnalyzer, UpdateDeltaTimeCodeFix>
	{
		[Fact]
		public async Task FixedUpdateWithDeltaTime()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
     public void FixedUpdate()
     {
         var foo = Time.deltaTime;
     }
}
";
			// see https://github.com/microsoft/Microsoft.Unity.Analyzers/issues/26
			// this rule is now disabled by default
			await VerifyCSharpDiagnosticAsync(test);

			// but can be re-enabled using ruleset or editorconfig:
			// dotnet_diagnostic.UNT0005.severity = suggestion

			/*var diagnostic = ExpectDiagnostic(UpdateDeltaTimeAnalyzer.FixedUpdateId)
				.WithLocation(8, 25);

			VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
     public void FixedUpdate()
     {
         var foo = Time.fixedDeltaTime;
     }
}
";
			await VerifyCSharpFixAsync(test, fixedTest);*/
		}

		[Fact]
		public async Task UpdateWithFixedDeltaTime()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
     public void Update()
     {
         var foo = Time.fixedDeltaTime;
     }
}
";

			var diagnostic = ExpectDiagnostic(UpdateDeltaTimeAnalyzer.UpdateId)
				.WithLocation(8, 25);

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
     public void Update()
     {
         var foo = Time.deltaTime;
     }
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}
	}
}
