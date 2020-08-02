/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class NullableReferenceTypesSuppressorTest : BaseSuppressorVerifierTest<NullableReferenceTypesSuppressor>
	{
		[Fact]
		public async Task NullableReferenceTypesSuppressed()
		{
			const string test =
@"
#nullable enable

using UnityEngine;

public class TestScript : MonoBehaviour
{
	private UnityEngine.Object field1;
	private GameObject field2;
	public UnityEngine.Object field3;

	public UnityEngine.Object Property1 { get; set; }
	public UnityEngine.Object Property2 { get { return property2Field; } set { property2Field = value; } }
	private UnityEngine.Object property2Field;

	[SerializeField] private GameObject serializedField;

	public static GameObject staticField;

	[HideInInspector] public GameObject hiddenField;
	[HideInInspector] public GameObject hiddenField2;

    private void Start()
    {
		field1 = new UnityEngine.Object();
		InitializeField2();

		Property1 = new UnityEngine.Object();
		Property2 = new UnityEngine.Object();

		staticField = new GameObject();

		hiddenField2 = new GameObject();
    }

	private void InitializeField2()
	{
		field2 = new GameObject();
	}
}
";

			DiagnosticResult[] suppressor =
			{
				ExpectSuppressor(NullableReferenceTypesSuppressor.NullableRule).WithLocation(8, 29), //field1
				ExpectSuppressor(NullableReferenceTypesSuppressor.NullableRule).WithLocation(9, 21), //field2
				ExpectSuppressor(NullableReferenceTypesSuppressor.NullableRule).WithLocation(10, 28), //field3
				ExpectSuppressor(NullableReferenceTypesSuppressor.NullableRule).WithLocation(12, 28), //Property1
				ExpectSuppressor(NullableReferenceTypesSuppressor.NullableRule).WithLocation(14, 29), //property2Field
				ExpectSuppressor(NullableReferenceTypesSuppressor.NullableRule).WithLocation(16, 38), //serializedField
				DiagnosticResult.CompilerWarning("CS0169").WithMessage("The field 'TestScript.serializedField' is never used").WithLocation(16, 38), //warning is not part of this analyzer
				ExpectSuppressor(NullableReferenceTypesSuppressor.NullableRule).WithLocation(18, 27), //staticField
				//ExpectDiagnostic().WithMessage("Non-nullable field 'hiddenField' is uninitialized. Consider declaring the field as nullable.").WithLocation(20, 38), //HOW TO WORK WITH SYSTEM DIAGNOSTICS?
				DiagnosticResult.CompilerWarning("CS8618").WithMessage("Non-nullable field 'hiddenField' is uninitialized. Consider declaring the field as nullable.")
					.WithLocation(20, 38), //should throw on public fields that are not shown in the inspector
				ExpectSuppressor(NullableReferenceTypesSuppressor.NullableRule).WithLocation(21, 38)
		};

			await VerifyCSharpDiagnosticAsync(test, suppressor);
		}
	}
}
