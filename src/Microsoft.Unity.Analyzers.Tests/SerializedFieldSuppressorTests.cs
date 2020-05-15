/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class SerializedFieldSuppressorTests : BaseSuppressorVerifierTest<SerializedFieldSuppressor>
	{
		[Fact]
		public async Task PrivateFieldWithAttributeNeverAssignedSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    readonly string someField; // we only use readonly here for testing purposes, suppressor is tested unitarily (so SerializedFieldSuppressor.ReadonlyRule [IDE0044] cannot be suppressed here)

    private void RemoveIDE0051() {
        var _ = someField;
        RemoveIDE0051();
    }
}
";

			var suppressor = ExpectSuppressor(SerializedFieldSuppressor.NeverAssignedRule)
				.WithLocation(7, 21);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task PrivateFieldWithAttributeReadonlySuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    private string someField = ""default"";

    private void RemoveIDE0051() {
        var _ = someField;
        RemoveIDE0051();
    }
}
";

			var suppressor = ExpectSuppressor(SerializedFieldSuppressor.ReadonlyRule)
				.WithLocation(7, 20);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task PublicFieldInSerializableTypeNeverAssignedSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public string someField;
}
";

			var suppressor = ExpectSuppressor(SerializedFieldSuppressor.NeverAssignedRule)
				.WithLocation(6, 19);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

		[Fact]
		public async Task PublicFieldInStandardTypeNeverAssigned()
		{
			const string test = @"
class Test : System.Object
{
    public string someField;
}
";

			// We don't want to suppress 'never assigned' for standard types
			await Assert.ThrowsAsync<TrueException>(async () =>
			{
				var suppressor = ExpectSuppressor(SerializedFieldSuppressor.NeverAssignedRule)
					.WithLocation(4, 19);

				await VerifyCSharpDiagnosticAsync(test, suppressor);
			});
		}


		[Fact]
		public async Task PrivateFieldWithAttributeUnusedSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    readonly string someField = ""default""; // we only use readonly here for testing purposes, suppressor is tested unitarily (so SerializedFieldSuppressor.ReadonlyRule [IDE0044] cannot be suppressed here)
}
";

			var suppressor = ExpectSuppressor(SerializedFieldSuppressor.UnusedRule)
				.WithLocation(7, 21);

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}

	}
}
