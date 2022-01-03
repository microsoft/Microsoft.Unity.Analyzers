﻿/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class NullableReferenceTypesSuppressorTest : BaseSuppressorVerifierTest<NullableReferenceTypesSuppressor>
{
	[Fact] async Task NonUnityClassIsExemptFromSuppressions()
	{
		const string test = @"
#nullable enable
using UnityEngine;

namespace Assets.Scripts
{
    public class Test /* is not a Unity object */
    {
        private GameObject field1;
        
        private GameObject property1 { get; set; }

        private void Start()
        {
            field1 = new GameObject();

            property1 = new GameObject();
        }
    }
}
";
	
		var context = AnalyzerVerificationContext.Default
			.WithLanguageVersion(LanguageVersion.CSharp8)
			.WithAnalyzerFilter("CS0169");

		DiagnosticResult[] diagnostics =
		{
			DiagnosticResult.CompilerWarning("CS8618")
				.WithMessage("Non-nullable field 'field1' must contain a non-null value when exiting constructor. Consider declaring the field as nullable.")
				.WithLocation(9, 28), 

			DiagnosticResult.CompilerWarning("CS8618")
				.WithMessage("Non-nullable property 'property1' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.")
				.WithLocation(11, 28), 
		};

		await VerifyCSharpDiagnosticAsync(context, test, diagnostics);
	}

	[Fact]
	public async Task NonUnityNullableReferenceTypesSuppressed()
	{
		const string test = @"
#nullable enable
using UnityEngine;

namespace Assets.Scripts
{
    public class Test : MonoBehaviour
    {
        private class NonUnityObject { }

        private GameObject field1;
        private NonUnityObject field2;
        
        private GameObject property1 { get; set; }
        private NonUnityObject property2 { get; set; }

        private void Start()
        {
            field1 = new GameObject();
            field2 = new NonUnityObject();

            property1 = new GameObject();
            property2 = new NonUnityObject();
        }
    }
}
"; 

		var context = AnalyzerVerificationContext.Default
			.WithLanguageVersion(LanguageVersion.CSharp8)
			.WithAnalyzerFilter("CS0169");

		DiagnosticResult[] suppressors =
		{
			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(11, 28), //field1
			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(12, 32), //field2
			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(14, 28), //property1
			ExpectSuppressor(NullableReferenceTypesSuppressor.Rule).WithLocation(15, 32), //property2
		};

		await VerifyCSharpDiagnosticAsync(context, test, suppressors);
	}

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
		DiagnosticResult[] suppressors =
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

		await VerifyCSharpDiagnosticAsync(context, test, suppressors);
	}
}
