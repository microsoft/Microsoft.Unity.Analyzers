/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class ShaderPropertyToIDTests : BaseCodeFixVerifierTest<ShaderPropertyToIDAnalyzer, ShaderPropertyToIDCodeFix>
{
	private const string ColorCall = "Shader.PropertyToID(\"_Color\")";
	private const string ColorField = "    private static readonly int ColorId = Shader.PropertyToID(\"_Color\");\n";

	[Theory]
	[InlineData("Shader.PropertyToID(\"_Color\")")]
	[InlineData("Shader.PropertyToID(name: \"_Color\")")]
	[InlineData("Shader.PropertyToID(@\"_Color\")")]
	[InlineData("UnityEngine.Shader.PropertyToID(\"_Color\")")]
	[InlineData("global::UnityEngine.Shader.PropertyToID(\"_Color\")")]
	public async Task LiteralCall(string expression)
	{
		var test = Source(expression, "0");
		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 16)
			.WithArguments("_Color");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
		await VerifyCSharpFixAsync(test, Source("ColorId", "0", ColorField));
	}

	[Fact]
	public async Task FixOnlySelectedCall()
	{
		await VerifyCSharpFixAsync(
			Source(ColorCall, ColorCall),
			Source("ColorId", ColorCall, ColorField),
			codeFixIndex: 0);
	}

	[Theory]
	[InlineData("struct Example")]
	[InlineData("record Example")]
	[InlineData("static class Example", "static ")]
	[InlineData("class Example<T> : MonoBehaviour")]
	public async Task TypeDeclarations(string declaration, string methodModifier = "")
	{
		var test = Source(ColorCall, ColorCall, declaration: declaration, methodModifier: methodModifier);
		var fixedTest = Source("ColorId", "ColorId", ColorField, declaration: declaration, methodModifier: methodModifier);

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Theory]
	[InlineData("using S = UnityEngine.Shader;", "S.PropertyToID(\"_Color\")", "S")]
	[InlineData("using static UnityEngine.Shader;", "PropertyToID(\"_Color\")", "")]
	public async Task AliasedAndImportedCalls(string extraUsing, string expression, string factoryType)
	{
		var test = Source(expression, expression, extraUsing: extraUsing);
		var factory = factoryType.Length == 0 ? "PropertyToID" : factoryType + ".PropertyToID";
		var field = $"    private static readonly int ColorId = {factory}(\"_Color\");\n";

		await VerifyCSharpFixAsync(test, Source("ColorId", "ColorId", field, extraUsing: extraUsing));
	}

	[Theory]
	[InlineData("Shader.PropertyToID(PropertyName)")]
	[InlineData("Shader.PropertyToID(\"_\" + \"Color\")")]
	[InlineData("Shader.PropertyToID(nameof(PropertyName))")]
	public async Task NonLiteralConstantsAreExcluded(string expression)
	{
		const string member = "    private const string PropertyName = \"_Color\";\n";
		await VerifyCSharpDiagnosticAsync(Source(expression, expression, member));
	}

	[Fact]
	public async Task LocalConstantsAreExcluded()
	{
		const string setup = "        const string propertyName = \"_Color\";\n";
		const string calls = "Shader.PropertyToID(propertyName) + Shader.PropertyToID(propertyName)";
		await VerifyCSharpDiagnosticAsync(Source(calls, "0", firstSetup: setup));
	}

	[Theory]
	[InlineData("ColorId")]
	[InlineData("ExistingColor")]
	public async Task ReuseMatchingField(string name)
	{
		var member = $"    private static readonly int {name} = Shader.PropertyToID(\"_Color\");\n";
		var test = Source(ColorCall, ColorCall, member);

		await VerifyCSharpFixAsync(test, Source(name, name, member));
	}

	[Theory]
	[InlineData("    private static readonly int ColorId = 42;\n")]
	[InlineData("    private static readonly int ColorId = Shader.PropertyToID(\"_Other\");\n")]
	[InlineData("    private static readonly int ColorId = Animator.StringToHash(\"_Color\");\n")]
	[InlineData("    private static int ColorId = Shader.PropertyToID(\"_Color\");\n")]
	[InlineData("    private readonly int ColorId = Shader.PropertyToID(\"_Color\");\n")]
	[InlineData("    private int ColorId => 42;\n")]
	public async Task DoNotReuseUnrelatedOrMutableMembers(string member)
	{
		var test = Source(ColorCall, ColorCall, member);
		var newField = "    private static readonly int ColorId1 = Shader.PropertyToID(\"_Color\");\n";
		if (member.Contains("=>"))
			newField += "\n";

		await VerifyCSharpFixAsync(test, Source("ColorId1", "ColorId1", newField + member));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AvoidLocalNameConflicts(bool existingField)
	{
		var member = existingField ? ColorField : "";
		var test = Source(ColorCall + " + ColorId", ColorCall, member, firstSetup: "        var ColorId = 42;\n");
		var newField = "    private static readonly int ColorId1 = Shader.PropertyToID(\"_Color\");\n";
		var fixedTest = Source("ColorId1 + ColorId", "ColorId1", newField + member, firstSetup: "        var ColorId = 42;\n");

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Theory]
	[InlineData("_Main-Tex", "MainTexId")]
	[InlineData("1st Texture", "_1stTextureId")]
	[InlineData("", "Id")]
	public async Task FieldNames(string property, string fieldName)
	{
		var call = $"Shader.PropertyToID(\"{property}\")";
		var field = $"    private static readonly int {fieldName} = {call};\n";

		await VerifyCSharpFixAsync(Source(call, call), Source(fieldName, fieldName, field));
	}

	[Fact]
	public async Task DifferentNamesWithTheSameFieldName()
	{
		var first = "Shader.PropertyToID(\"_Main-Tex\") + Shader.PropertyToID(\"_Main Tex\")";
		var test = Source(first, first);
		const string fields =
			"    private static readonly int MainTexId1 = Shader.PropertyToID(\"_Main Tex\");\n" +
			"    private static readonly int MainTexId = Shader.PropertyToID(\"_Main-Tex\");\n";
		var fixedTest = Source("MainTexId + MainTexId1", "MainTexId + MainTexId1", fields);

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task ReuseFieldFromAnotherPartialDeclaration()
	{
		const string otherPart = @"
partial class Example
{
    private static readonly int ExistingColor = Shader.PropertyToID(""_Color"");
}";
		var test = Source(ColorCall, ColorCall, declaration: "partial class Example : MonoBehaviour") + otherPart;
		var fixedTest = Source("ExistingColor", "ExistingColor", declaration: "partial class Example : MonoBehaviour") + otherPart;

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task CallsInOneMethod()
	{
		var test = Source(ColorCall + " + " + ColorCall, "0");
		var first = ExpectDiagnostic().WithLocation(8, 16).WithArguments("_Color");
		var second = ExpectDiagnostic().WithLocation(8, 16 + ColorCall.Length + 3).WithArguments("_Color");

		await VerifyCSharpDiagnosticAsync(test, first, second);
		await VerifyCSharpFixAsync(test, Source("ColorId + ColorId", "0", ColorField));
	}

	[Theory]
	[InlineData("_Other", "OtherId")]
	[InlineData("_color", "ColorId1")]
	public async Task DifferentPropertyNames(string name, string fieldName)
	{
		var call = $"Shader.PropertyToID(\"{name}\")";
		var test = Source(ColorCall, call);
		var first = ExpectDiagnostic().WithLocation(8, 16).WithArguments("_Color");
		var second = ExpectDiagnostic().WithLocation(13, 16).WithArguments(name);
		var field = $"    private static readonly int {fieldName} = {call};\n";

		await VerifyCSharpDiagnosticAsync(test, first, second);
		await VerifyCSharpFixAsync(test, Source("ColorId", fieldName, field + ColorField));
	}

	[Fact]
	public async Task PreservesComments()
	{
		var test = Source("Shader.PropertyToID(/* property */ \"_Color\") /* first */", ColorCall + " /* second */");
		var fixedTest = Source("/* property */ ColorId /* first */", "ColorId /* second */", ColorField);

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Theory]
	[InlineData("Shader.PropertyToID(null)", "Shader.PropertyToID(null)")]
	[InlineData("Shader.PropertyToID(PropertyName)", "Shader.PropertyToID(PropertyName)", "    private string PropertyName = \"_Color\";\n")]
	[InlineData("ColorId", "0", ColorField)]
	[InlineData("ColorId", "OtherColorId", ColorField + "    private static readonly int OtherColorId = Shader.PropertyToID(\"_Color\");\n")]
	public async Task NonLiteralOrCachedCalls(string first, string second, string members = "")
	{
		await VerifyCSharpDiagnosticAsync(Source(first, second, members));
	}

	[Fact]
	public async Task SingleCallInUpdate()
	{
		const string test = @"
using UnityEngine;

class Example : MonoBehaviour
{
    void Update()
    {
        Debug.Log(Shader.PropertyToID(""_Color""));
    }
}";

		var diagnostic = ExpectDiagnostic().WithLocation(8, 19).WithArguments("_Color");
		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Example : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID(""_Color"");

    void Update()
    {
        Debug.Log(ColorId);
    }
}";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Theory]
	[InlineData("Shader.PropertyToID(\"_Color\"); Shader.PropertyToID(\"_Color\");")]
	[InlineData("System.Action action = () => Shader.PropertyToID(\"_Color\"); action();")]
	[InlineData("for (Shader.PropertyToID(\"_Color\"); System.Environment.TickCount < 0; Shader.PropertyToID(\"_Color\")) { }")]
	public async Task DiscardedValues(string statement)
	{
		var test = Source("0", "0", firstSetup: "        " + statement + "\n");
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task ExpressionBodiedVoidMethod()
	{
		var test = Source("0", "0", "    void Method() => Shader.PropertyToID(\"_Color\");\n");
		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task UnrelatedMethod()
	{
		const string otherType = @"
static class OtherShader
{
    public static int PropertyToID(string name) => 0;
}";
		var test = Source("OtherShader.PropertyToID(\"_Color\")", "OtherShader.PropertyToID(\"_Color\")") + otherType;

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task SingleCallInNestedType()
	{
		const string test = @"
using UnityEngine;

class Example
{
    class Nested
    {
        int Read() => Shader.PropertyToID(""_Color"");
    }
}";
		var diagnostic = ExpectDiagnostic().WithLocation(8, 23).WithArguments("_Color");
		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Example
{
    class Nested
    {
        private static readonly int ColorId = Shader.PropertyToID(""_Color"");

        int Read() => ColorId;
    }
}";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task CallsInDifferentPartialDeclarations()
	{
		const string otherPart = @"
partial class Example
{
    int Read() => Shader.PropertyToID(""_Color"");
}";
		var test = Source(ColorCall, "0", declaration: "partial class Example : MonoBehaviour") + otherPart;

		var fixedTest = Source("ColorId", "0", ColorField, declaration: "partial class Example : MonoBehaviour")
			+ otherPart.Replace(ColorCall, "ColorId");
		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task PropertyInitializersAreExcluded()
	{
		const string properties =
			"    int ColorId { get; } = Shader.PropertyToID(\"_Color\");\n" +
			"    int OtherColorId { get; } = Shader.PropertyToID(\"_Color\");\n";
		await VerifyCSharpDiagnosticAsync(Source("ColorId", "OtherColorId", properties));
	}

	[Fact]
	public void NoIndependentBatchFieldGeneration()
	{
		Assert.Null(new ShaderPropertyToIDCodeFix().GetFixAllProvider());
	}

	private static string Source(string first, string second, string members = "", string declaration = "class Example : MonoBehaviour",
		string extraUsing = "", string methodModifier = "", string firstSetup = "")
	{
		if (members.Length > 0)
			members += "\n";

		return $@"
using UnityEngine;
{extraUsing}
{declaration}
{{
{members}    {methodModifier}int First()
    {{
{firstSetup}        return {first};
    }}

    {methodModifier}int Second()
    {{
        return {second};
    }}
}}
";
	}
}
