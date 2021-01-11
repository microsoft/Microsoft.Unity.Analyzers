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
		collider.gameObject.GetComponent<Rigidbody>();
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
		collider.GetComponent<Rigidbody>();
	}
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task RemoveTransformProperty()
		{
			const string test = @"
using UnityEditor;

class Camera : MonoBehaviour
{
     private void Awake()
	{
		transform.name = ""Title"";
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
		name = ""Title"";
	}

}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}
	}
}
