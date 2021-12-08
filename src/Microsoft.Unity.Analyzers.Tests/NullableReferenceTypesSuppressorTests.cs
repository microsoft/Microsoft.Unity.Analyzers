/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class NullableReferenceTypesSuppressorTest : BaseSuppressorVerifierTest<NullableReferenceTypesSuppressor>
{
	[Fact]
	public async Task NullableReferenceTypesSuppressed()
	{
		const string test = @"
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
			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(8, 29), //field1
			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(9, 21), //field2
			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(10, 28), //field3
			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(12, 28), //Property1
			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(14, 29), //property2Field
			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(16, 38), //serializedField

			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(18, 27), //staticField

			DiagnosticResult.CompilerWarning("CS8618")
				.WithMessage("Non-nullable field 'hiddenField' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.")
				.WithLocation(20, 38), //should throw on public fields that are not shown in the inspector

			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(21, 38)
		};

		var context = AnalyzerVerificationContext.Default
			.WithLanguageVersion(LanguageVersion.CSharp8)
			.WithAnalyzerFilter("CS0169");

		await VerifyCSharpDiagnosticAsync(context, test, suppressor);
	}
}
