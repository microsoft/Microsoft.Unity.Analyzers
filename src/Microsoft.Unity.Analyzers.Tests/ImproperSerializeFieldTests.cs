using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<ImproperSerializeFieldAnalyzer, ImproperSerializeFieldCodeFix>;

	public class ImproperSerializeFieldTests
	{
		[Fact]
		public async Task ValidSerializeFieldTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    private string privateField;
}
";

			await Verify.VerifyAnalyzerAsync(test);
		}

		[Fact]
		public async Task RedundantSerializeFieldTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    public string publicField;
}
";

			var diagnostic = Verify.Diagnostic(ImproperSerializeFieldAnalyzer.Id).WithLocation(6, 5).WithArguments("publicField");

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public string publicField;
}
";

			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task InvalidSerializeFieldTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    private string privateProperty { get; set; } 
}
";

			var diagnostic = Verify.Diagnostic(ImproperSerializeFieldAnalyzer.Id).WithLocation(6, 5).WithArguments("privateProperty");

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private string privateProperty { get; set; } 
}
";

			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task ValidSerializeMultipleFieldsTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    private string privateField1, privateField2, privateField3;
}
";

			await Verify.VerifyAnalyzerAsync(test);
		}

		[Fact]
		public async Task RedundantSerializeMultipleFieldsTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    public string publicField1, publicField2, publicField3;
}
";

			var diagnostic = Verify.Diagnostic(ImproperSerializeFieldAnalyzer.Id).WithLocation(6, 5).WithArguments("publicField1, publicField2, publicField3");

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public string publicField1, publicField2, publicField3;
}
";

			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}
	}
}
