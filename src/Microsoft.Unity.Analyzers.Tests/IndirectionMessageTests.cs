/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class IndirectionMessageTests : BaseCodeFixVerifierTest<IndirectionMessageAnalyzer, IndirectionMessageCodeFix>
	{
		[Fact]
		public async Task RemoveGameObjectProperty()
		{
			const string test = @"
using UnityEditor;

class Camera : MonoBehaviour
{
     private void OnTriggerEnter(Collider collider)
	{
		GameObject original = null;
		GameObject duplicate = original.gameObject;
	}
}
";

			var diagnostic = ExpectDiagnostic(CreateInstanceAnalyzer.ScriptableObjectId).WithLocation(9, 19).WithArguments("Foo");
			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEditor;

class Camera : MonoBehaviour
{
     private void OnTriggerEnter(Collider collider)
	{
		GameObject original = null;
		GameObject duplicate = original;
	}
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}
	}
}
