using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
    using Verify = UnityCodeFixVerifier<UpdateWithoutFixedDeltaTimeAnalyzer, UpdateWithoutFixedDeltaTimeCodeFix>;

    public class UpdateWithoutFixedDeltaTimeTests
    {
        [Fact]
        public async Task UpdateWithFixedDeltaTime ()
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

            var diagnostic = Verify.Diagnostic().WithLocation(8, 25);

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
            await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
        }
    }
}
