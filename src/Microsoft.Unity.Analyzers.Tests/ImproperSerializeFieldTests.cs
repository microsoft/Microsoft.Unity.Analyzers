/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class ImproperSerializeFieldTests : BaseCodeFixVerifierTest<ImproperSerializeFieldAnalyzer, ImproperSerializeFieldCodeFix>
	{
		[Fact]
		public void ValidSerializeFieldTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    private string privateField;
}
";

			VerifyCSharpDiagnostic(test);
		}

		[Fact]
		public void RedundantSerializeFieldTest()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    [SerializeField]
    public string publicField;
}
";

			var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
				.WithLocation(6, 5)
				.WithArguments("publicField");

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    public string publicField;
}
";

			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void InvalidSerializeFieldTest()
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

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    private string privateProperty { get; set; } 
}
";

			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void ValidSerializeMultipleFieldsTest()
		{
			const string test = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [SerializeField]
    private string privateField1, privateField2, privateField3;
}
";

			VerifyCSharpDiagnostic(test);
		}

		[Fact]
		public void RedundantSerializeMultipleFieldsTest()
		{
			const string test = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [SerializeField]
    public string publicField1, publicField2, publicField3;
}
";

			var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
				.WithLocation(7, 5)
				.WithArguments("publicField1, publicField2, publicField3");

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    public string publicField1, publicField2, publicField3;
}
";

			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void ValidSerializeFieldMultipleAttributesTest()
		{
			const string test = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [SerializeField]
    [FormerlySerializedAs(""somethingElse"")]
    private string privateField;
}
";

			VerifyCSharpDiagnostic(test);
		}

		[Fact]
		public void RedundantSerializeFieldMultipleAttributeTest()
		{
			const string test = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [SerializeField]
    [FormerlySerializedAs(""somethingElse"")]
    public string publicField;
}
";

			var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
				.WithLocation(7, 5)
				.WithArguments("publicField");

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [FormerlySerializedAs(""somethingElse"")]
    public string publicField;
}
";


			VerifyCSharpFix(test, fixedTest);
		}

		[Fact]
		public void ValidSerializeFieldMultipleAttributeInlineTest()
		{
			const string test = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs(""somethingElse"")]
    private string privateField;
}
";

			VerifyCSharpDiagnostic(test);
		}

		[Fact]
		public void RedundantSerializeFieldMultipleAttributeInlineTest()
		{
			const string test = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs(""somethingElse"")]
    public string publicField;
}
";

			var diagnostic = ExpectDiagnostic(ImproperSerializeFieldAnalyzer.Rule.Id)
				.WithLocation(7, 5)
				.WithArguments("publicField");

			VerifyCSharpDiagnostic(test, diagnostic);

			const string fixedTest = @"
using UnityEngine;
using UnityEngine.Serialization;

class Camera : MonoBehaviour
{
    [FormerlySerializedAs(""somethingElse"")]
    public string publicField;
}
";

			VerifyCSharpFix(test, fixedTest);
		}
	}
}
