/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class CreateInstanceTests : BaseCodeFixVerifierTest<CreateInstanceAnalyzer, CreateInstanceCodeFix>
	{
		[Fact]
		public void CreateMonoBehaviourInstance()
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

			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void CreateMonoBehaviourInstanceFromNonComponent()
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
			VerifyCSharpDiagnostic(test, diagnostic);
		}

		[Fact]
		public void CreateScriptableObjectInstance()
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
			VerifyCSharpDiagnostic(test, diagnostic);

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
			VerifyCSharpFix(test, fixedTest);
		}
	}
}
