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
		public async Task CreateComponentInstance()
		{
			const string test = @"
using UnityEngine;

class Foo : Component { }

class Camera : MonoBehaviour
{
    public void Update() {
        Foo foo = new Foo();
    }
}
";

			var diagnostic = ExpectDiagnostic(CreateInstanceAnalyzer.ComponentId)
				.WithLocation(9, 19)
				.WithArguments("Foo");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Foo : Component { }

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
		public async Task CreateComponentInstanceArgumentOrParenthesis()
		{
			const string test = @"
using UnityEngine;

class Foo : Component { }

class Camera : MonoBehaviour
{
    public void Method(Foo foo) {
        Method(((new Foo())));
    }
}
";

			var diagnostic = ExpectDiagnostic(CreateInstanceAnalyzer.ComponentId)
				.WithLocation(9, 18)
				.WithArguments("Foo");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Foo : Component { }

class Camera : MonoBehaviour
{
    public void Method(Foo foo) {
        Method(((gameObject.AddComponent<Foo>())));
    }
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}


		[Fact]
		public async Task CreateComponentInstanceComments()
		{
			const string test = @"
using UnityEngine;

class Foo : Component { }

class Camera : MonoBehaviour
{
    public void Update() {
        // comment
        Foo foo = /* inner */ new Foo();
        /* comment */
    }
}
";

			var diagnostic = ExpectDiagnostic(CreateInstanceAnalyzer.ComponentId)
				.WithLocation(10, 31)
				.WithArguments("Foo");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Foo : Component { }

class Camera : MonoBehaviour
{
    public void Update() {
        // comment
        Foo foo = /* inner */ gameObject.AddComponent<Foo>();
        /* comment */
    }
}
";
			await VerifyCSharpFixAsync(test, fixedTest);
		}

		[Fact]
		public async Task CreateMonoBehaviourInstance()
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

			var diagnostic = ExpectDiagnostic(CreateInstanceAnalyzer.ComponentId)
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
		public async Task CreateMonoBehaviourInstanceFromNonComponent()
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
			var diagnostic = ExpectDiagnostic(CreateInstanceAnalyzer.ComponentId)
				.WithLocation(9, 19)
				.WithArguments("Foo");

			await VerifyCSharpDiagnosticAsync(test, diagnostic);
		}

		[Fact]
		public async Task CreateScriptableObjectInstance()
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

			var diagnostic = ExpectDiagnostic(CreateInstanceAnalyzer.ScriptableObjectId)
				.WithLocation(9, 19)
				.WithArguments("Foo");

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
