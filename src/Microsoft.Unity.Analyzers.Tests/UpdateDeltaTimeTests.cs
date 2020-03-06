/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class UpdateWithoutDeltaTimeTests : BaseCodeFixVerifierTest<UpdateDeltaTimeAnalyzer, UpdateDeltaTimeCodeFix>
	{
		[Fact]
		public void FixedUpdateWithDeltaTime()
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
			VerifyCSharpDiagnostic(test);

			// but can be re-enabled using ruleset or editorconfig:
			// dotnet_diagnostic.UNT0005.severity = suggestion
			
			/*var diagnostic = ExpectDiagnostic(UpdateDeltaTimeAnalyzer.FixedUpdateId)
				.WithLocation(8, 25);

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);*/
		}

		[Fact]
		public void UpdateWithFixedDeltaTime()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}
	}
}
