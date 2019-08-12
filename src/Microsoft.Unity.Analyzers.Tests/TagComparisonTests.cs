using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<TagComparisonAnalyzer, TagComparisonCodeFix>;

	public class TagComparisonTests
	{
		[Fact]
		public async Task TagAsIdentifier()
		{
			var test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(tag == ""tag1"");
    }
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(8, 19);

			var fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(CompareTag(""tag1""));
    }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task TagProperty()
		{
			var test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    public Transform transform;

    private void Update()
    {
        Debug.Log(transform.tag == ""tag2"");
    }
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(10, 19);

			var fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    public Transform transform;

    private void Update()
    {
        Debug.Log(transform.CompareTag(""tag2""));
    }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task TagAsIdentifierRhs()
		{
			var test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(""tag3"" == tag);
    }
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(8, 19);

			var fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    private void Update()
    {
        Debug.Log(CompareTag(""tag3""));
    }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}

		[Fact]
		public async Task TagPropertyRhs()
		{
			var test = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    public Transform transform;

    private void Update()
    {
        Debug.Log(""tag4"" == transform.tag);
    }
}
";

			var diagnostic = Verify.Diagnostic().WithLocation(10, 19);

			var fixedTest = @"
using UnityEngine;

public class Camera : MonoBehaviour
{
    public Transform transform;

    private void Update()
    {
        Debug.Log(transform.CompareTag(""tag4""));
    }
}
";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}
	}
}
