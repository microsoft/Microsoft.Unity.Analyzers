/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class CreateInstanceTests : BaseCodeFixVerifierTest<CreateInstanceAnalyzer, CreateInstanceCodeFix>
	{
		[Fact]
		public async Task CreateMonoBehaviourInstanceAsync()
		{
			const string test = @"
using UnityEngine;

class Foo : MonoBehaviour { }

class Camera : MonoBehaviour
{
    public void Update() {
        Foo foo = new Foo();
    }
}
";

			var diagnostic = ExpectDiagnostic(CreateInstanceAnalyzer.MonoBehaviourId)
				.WithLocation(9, 19)
				.WithArguments("Foo");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Foo : MonoBehaviour { }

class Camera : MonoBehaviour
{
    public void Update() {
        Foo foo = gameObject.AddComponent<Foo>();
    }
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task CreateMonoBehaviourInstanceFromNonComponentAsync()
		{
			const string test = @"
using UnityEngine;

class Foo : MonoBehaviour { }

class Program
{
    public void Main() {
        Foo foo = new Foo();
    }
}
";
			var diagnostic = ExpectDiagnostic(CreateInstanceAnalyzer.MonoBehaviourId).WithLocation(9, 19).WithArguments("Foo");
			await VerifyCSharpDiagnosticAsync(test, diagnostic);
		}

		[Fact]
		public async Task CreateScriptableObjectInstanceAsync()
		{
			const string test = @"
using UnityEngine;

class Foo : ScriptableObject { }

class Camera : MonoBehaviour
{
    public void Update() {
        Foo foo = new Foo();
    }
}
";

			var diagnostic = ExpectDiagnostic(CreateInstanceAnalyzer.ScriptableObjectId).WithLocation(9, 19).WithArguments("Foo");
			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Foo : ScriptableObject { }

class Camera : MonoBehaviour
{
    public void Update() {
        Foo foo = ScriptableObject.CreateInstance<Foo>();
    }
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}
	}
}
