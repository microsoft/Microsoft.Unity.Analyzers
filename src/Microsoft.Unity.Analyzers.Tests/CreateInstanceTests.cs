using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<CreateInstanceAnalyzer, CreateInstanceCodeFix>;

	public class CreateInstanceTests
	{
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

			var diagnostic = Verify.Diagnostic(CreateInstanceAnalyzer.MonoBehaviourId).WithLocation(9, 19).WithArguments("Foo");

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
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
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
			var diagnostic = Verify.Diagnostic(CreateInstanceAnalyzer.MonoBehaviourId).WithLocation(9, 19).WithArguments("Foo");

			// We report the diagnostic but can not offer a codefix

			const string fixedTest = @"
using UnityEngine;

class Foo : MonoBehaviour { }

class Program
{
    public void Main() {
        Foo foo = new Foo();
    }
}
";

			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
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

			var diagnostic = Verify.Diagnostic(CreateInstanceAnalyzer.ScriptableObjectId).WithLocation(9, 19).WithArguments("Foo");

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
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}
	}
}