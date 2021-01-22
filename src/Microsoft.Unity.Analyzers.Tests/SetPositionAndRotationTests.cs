/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class SetPositionAndRotationTests : BaseCodeFixVerifierTest<SetPositionAndRotationAnalyzer, SetPositionAndRotationCodeFix>
	{
		[Fact]
		public async Task Test()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	void Update()
	{
		transform.position = new Vector3(0.0f, 1.0f, 0.0f);
		transform.position = new Vector3(0.0f, 1.0f, 0.0f);
	}
}
";

			var diagnostic = ExpectDiagnostic().WithLocation(9, 3);
			
			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
	void Update()
	{
	transform.SetPositionAndRotation(new Vector3(0.0f, 1.0f, 0.0f, transform.rotation);
	}
}
";

			await VerifyCSharpFixAsync(test, fixedTest);
		}
	}
}
