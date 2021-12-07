/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class ImproperSerializeFieldTests : BaseCodeFixVerifierTest<ImproperSerializeFieldAnalyzer, ImproperSerializeFieldCodeFix>
{
	[Fact]
	public async Task ValidSerializeFieldTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    private string privateField = null;
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task RedundantSerializeFieldTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    public string publicField = null;
}
";

		var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
			.WithLocation(6, 5)
			.WithArguments("publicField");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public string publicField = null;
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task RedundantSerializeFieldComments()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    // comment
    [SerializeField]
    public string publicField = null; /* comment */
}
";

		var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
			.WithLocation(7, 5)
			.WithArguments("publicField");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    // comment
    public string publicField = null; /* comment */
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task InvalidSerializeFieldPropertyTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    private string privateProperty { get; set; } 
}
";

		var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
			.WithLocation(6, 5)
			.WithArguments("privateProperty");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private string privateProperty { get; set; } 
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task InvalidSerializeFieldReadonlyTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    readonly string readonlyField;
}
";

		var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
			.WithLocation(6, 5)
			.WithArguments("readonlyField");

		var context = AnalyzerVerificationContext.Default
			.WithAnalyzerFilter("CS0169");

		await VerifyCSharpDiagnosticAsync(context, test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    readonly string readonlyField;
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task InvalidSerializeFieldStaticTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    static string staticField;
}
";

		var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
			.WithLocation(6, 5)
			.WithArguments("staticField");

		var context = AnalyzerVerificationContext.Default
			.WithAnalyzerFilter("CS0169");

		await VerifyCSharpDiagnosticAsync(context, test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    static string staticField;
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
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

    private void _ ()
    {
        privateField1 = privateField2 = privateField3 = privateField1;
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task RedundantSerializeMultipleFieldsTest()
	{
		const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    public string publicField1 = null, publicField2 = null, publicField3 = null;
}
";

		var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
			.WithLocation(6, 5)
			.WithArguments("publicField1");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public string publicField1 = null, publicField2 = null, publicField3 = null;
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task ValidSerializeFieldMultipleAttributesTest()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [SerializeField]
    [FormerlySerializedAs(""somethingElse"")]
    private string privateField = null;
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task RedundantSerializeFieldMultipleAttributeTest()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [SerializeField]
    [FormerlySerializedAs(""somethingElse"")]
    public string publicField = null;
}
";

		var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
			.WithLocation(7, 5)
			.WithArguments("publicField");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [FormerlySerializedAs(""somethingElse"")]
    public string publicField = null;
}
";


		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task ValidSerializeFieldMultipleAttributeInlineTest()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs(""somethingElse"")]
    private string privateField = null;
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task RedundantSerializeFieldMultipleAttributeInlineTest()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs(""somethingElse"")]
    public string publicField = null;
}
";

		var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
			.WithLocation(7, 5)
			.WithArguments("publicField");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [FormerlySerializedAs(""somethingElse"")]
    public string publicField = null;
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
