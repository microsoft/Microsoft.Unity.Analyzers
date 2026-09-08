/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class TextMeshProSetTextTests : BaseDiagnosticVerifierTest<TextMeshProSetTextAnalyzer>
{
	private readonly AnalyzerVerificationContext _context = AnalyzerVerificationContext.Default
		.WithAnalyzerFilter("CS8019"); // Shared test sources can contain unnecessary using directives.

	[Theory]
	[InlineData("TMP_Text", "label.text = builder.ToString();")]
	[InlineData("TextMeshPro", "label.text = builder.ToString();")]
	[InlineData("TextMeshProUGUI", "label.text = builder.ToString();")]
	[InlineData("TMP_Text", "label.SetText(builder.ToString());")]
	[InlineData("TextMeshPro", "label.SetText(builder.ToString());")]
	[InlineData("TextMeshProUGUI", "label.SetText(builder.ToString());")]
	[InlineData("TMP_Text", "label.SetText(sourceText: builder.ToString());")]
	[InlineData("TMP_Text", "label?.SetText(builder.ToString());", 15)]
	[InlineData("TMP_Text", "label.text = (builder.ToString());")]
	[InlineData("TMP_Text", "label.SetText((builder.ToString()));")]
	[InlineData("TMP_Text", "label.text = new StringBuilder().Append(\"Score\").ToString();")]
	public async Task StringBuilder(string textType, string statement, int column = 9)
	{
		await VerifyCSharpDiagnosticAsync(_context,
			CreateSource(statement, "StringBuilder builder", textType),
			ExpectDiagnostic().WithLocation(9, column).WithArguments("SetText(StringBuilder)"));
	}

	[Theory]
	[InlineData("label.text = new string(buffer);", "SetText(char[])")]
	[InlineData("label.SetText(new string(buffer));", "SetText(char[])")]
	[InlineData("label.text = new(buffer);", "SetText(char[])")]
	[InlineData("label.text = new string(buffer, start, count);", "SetText(char[], int, int)")]
	[InlineData("label.SetText(new string(buffer, start, count));", "SetText(char[], int, int)")]
	[InlineData("label.text = new string(value: buffer, startIndex: start, length: count);", "SetText(char[], int, int)")]
	[InlineData("label.text = new string(length: count, value: buffer, startIndex: start);", "SetText(char[], int, int)")]
	public async Task CharacterArray(string statement, string overload)
	{
		await VerifyCSharpDiagnosticAsync(_context,
			CreateSource(statement, "char[] buffer, int start, int count"),
			ExpectDiagnostic().WithLocation(9, 9).WithArguments(overload));
	}

	[Theory]
	[InlineData("byte")]
	[InlineData("sbyte")]
	[InlineData("short")]
	[InlineData("ushort")]
	[InlineData("int")]
	[InlineData("uint")]
	[InlineData("float")]
	public async Task NumericInterpolation(string type)
	{
		await VerifyCSharpDiagnosticAsync(_context,
			CreateSource("label.text = $\"Score: {value}\";", $"{type} value"),
			ExpectDiagnostic().WithLocation(9, 9).WithArguments("SetText(string, float)"));
	}

	[Theory]
	[InlineData("label.SetText($\"Score: {value}\");")]
	[InlineData("label.SetText(sourceText: $\"Score: {value}\");")]
	[InlineData("label.text = value.ToString();")]
	[InlineData("label.SetText(value.ToString());")]
	[InlineData("label.text = $@\"Score: {value}\";")]
	public async Task NumericText(string statement)
	{
		await VerifyCSharpDiagnosticAsync(_context,
			CreateSource(statement, "float value"),
			ExpectDiagnostic().WithLocation(9, 9).WithArguments("SetText(string, float)"));
	}

	[Theory]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	[InlineData(8)]
	public async Task MultipleNumericArguments(int count)
	{
		var content = string.Join(" ", Enumerable.Repeat("{value}", count));
		await VerifyCSharpDiagnosticAsync(_context,
			CreateSource($"label.text = $\"{content}\";", "float value"),
			ExpectDiagnostic().WithLocation(9, 9).WithArguments("SetText(string, float, ...)"));
	}

	[Theory]
	[InlineData("label.text = \"Score\";")]
	[InlineData("label.text = text;")]
	[InlineData("label.text = null;")]
	[InlineData("label.text = $\"Score\";")]
	[InlineData("label.text = $\"Name: {text}\";")]
	[InlineData("label.text = $\"{text}: {value}\";")]
	[InlineData("label.SetText(text);")]
	[InlineData("label.SetText((string)null);")]
	[InlineData("label.SetText(\"Score: {0}\", value);")]
	[InlineData("label.SetText(builder);")]
	[InlineData("label.SetText(buffer);")]
	[InlineData("label.SetText(buffer, 0, buffer.Length);")]
	public async Task NoTemporaryStringOrNoSuitableOverload(string statement)
	{
		await VerifyCSharpDiagnosticAsync(_context, CreateSource(statement, "string text, float value, StringBuilder builder, char[] buffer"));
	}

	[Theory]
	[InlineData("label.text = builder.ToString(0, 1);")]
	[InlineData("label.text = builder?.ToString();")]
	[InlineData("label.text += builder.ToString();")]
	[InlineData("label.SetText(builder.ToString(), 1f);")]
	[InlineData("label.SetText(arg0: 1f, sourceText: builder.ToString());")]
	[InlineData("label.text = new string('a', 10);")]
	[InlineData("label.text = new string((char[])null);")]
	[InlineData("label.text = new string(value: (char[])null);")]
	[InlineData("var copy = new TextMeshProUGUI { text = builder.ToString() };")]
	public async Task UnsupportedBufferUsage(string statement)
	{
		await VerifyCSharpDiagnosticAsync(_context, CreateSource(statement, "StringBuilder builder"));
	}

	[Theory]
	[InlineData("double")]
	[InlineData("decimal")]
	[InlineData("long")]
	[InlineData("ulong")]
	[InlineData("float?")]
	[InlineData("bool")]
	[InlineData("char")]
	[InlineData("object")]
	public async Task UnsupportedNumericType(string type)
	{
		await VerifyCSharpDiagnosticAsync(_context, CreateSource("label.text = $\"Value: {value}\";", $"{type} value"));
		await VerifyCSharpDiagnosticAsync(_context, CreateSource("label.text = value.ToString();", $"{type} value"));
	}

	[Theory]
	[InlineData("label.text = $\"{value:F2}\";")]
	[InlineData("label.text = $\"{value:0.00}\";")]
	[InlineData("label.text = $\"{value,10}\";")]
	[InlineData("label.text = $\"{{Score}}: {value}\";")]
	[InlineData("label.text = value.ToString(\"F2\");")]
	[InlineData("label.text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);")]
	[InlineData("label.text = string.Format(\"{0:N2}\", value);")]
	[InlineData("label.text = $\"{value} {value} {value} {value} {value} {value} {value} {value} {value}\";")]
	[InlineData("label.SetText($\"{value}\", 1f);")]
	public async Task UnsupportedFormatting(string statement)
	{
		await VerifyCSharpDiagnosticAsync(_context, CreateSource(statement, "float value"));
	}

	[Theory]
	[InlineData("16777217")]
	[InlineData("16777217u")]
	[InlineData("12345678")]
	[InlineData("12345678u")]
	[InlineData("12345678f")]
	[InlineData("int.MaxValue")]
	[InlineData("uint.MaxValue")]
	[InlineData("1e20f")]
	[InlineData("float.MaxValue")]
	[InlineData("float.MinValue")]
	[InlineData("float.NaN")]
	[InlineData("float.PositiveInfinity")]
	[InlineData("float.NegativeInfinity")]
	public async Task KnownNumericRangeOrPrecisionLoss(string value)
	{
		await VerifyCSharpDiagnosticAsync(_context, CreateSource($"label.text = $\"Value: {{{value}}}\";", "float unused"));
	}

	[Fact]
	public async Task DerivedTextType()
	{
		var source = CreateSource("label.text = builder.ToString();", "StringBuilder builder", "CustomText")
			+ @"
class CustomText : TextMeshProUGUI
{
}
";
		await VerifyCSharpDiagnosticAsync(_context, source, ExpectDiagnostic().WithLocation(9, 9).WithArguments("SetText(StringBuilder)"));
	}

	[Theory]
	[InlineData("label.text = builder.ToString();", "public override string text { get; set; }")]
	[InlineData("label.text = builder.ToString();", "public new string text { get; set; }")]
	[InlineData("label.SetText(builder.ToString());", "public new void SetText(string sourceText, bool syncTextInputBox = true) { }")]
	public async Task CustomTextMembers(string statement, string member)
	{
		var source = CreateSource(statement, "StringBuilder builder", "CustomText")
			+ $@"
class CustomText : TextMeshProUGUI
{{
    {member}
}}
";
		await VerifyCSharpDiagnosticAsync(_context, source);
	}

	[Theory]
	[InlineData("label.text = builder.ToString();")]
	[InlineData("label.SetText(builder.ToString());")]
	public async Task UnrelatedType(string statement)
	{
		var source = CreateSource(statement, "StringBuilder builder", "OtherText")
			+ @"
class OtherText
{
    public string text { get; set; }
    public void SetText(string value) { }
}
";
		await VerifyCSharpDiagnosticAsync(_context, source);
	}

	[Fact]
	public async Task AssignmentValueIsUsed()
	{
		const string source = @"
using System.Text;
using TMPro;

class Example
{
    string UpdateText(TMP_Text label, StringBuilder builder)
    {
        return label.text = builder.ToString();
    }
}
";
		await VerifyCSharpDiagnosticAsync(_context, source);
	}

	[Fact]
	public async Task ImplicitReceiver()
	{
		const string source = @"
using System.Text;
using TMPro;

class Example : TextMeshProUGUI
{
    void UpdateText(StringBuilder builder)
    {
        text = builder.ToString();
        SetText(builder.ToString());
    }
}
";
		await VerifyCSharpDiagnosticAsync(_context, source,
			ExpectDiagnostic().WithLocation(9, 9).WithArguments("SetText(StringBuilder)"),
			ExpectDiagnostic().WithLocation(10, 9).WithArguments("SetText(StringBuilder)"));
	}

	[Fact]
	public async Task Trivia()
	{
		await VerifyCSharpDiagnosticAsync(_context,
			CreateSource("label.text = /* source */ builder.ToString(); // display", "StringBuilder builder"),
			ExpectDiagnostic().WithLocation(9, 9).WithArguments("SetText(StringBuilder)"));
	}

	[Fact]
	public async Task TextMeshProNotReferenced()
	{
		var context = AnalyzerVerificationContext.Default;
		var document = CreateDocument(context, "class Example { }");
		var references = document.Project.MetadataReferences
			.Where(reference => reference.Display?.EndsWith("Unity.TextMeshPro.dll", StringComparison.OrdinalIgnoreCase) != true);
		var project = document.Project.WithMetadataReferences(references);
		var updatedDocument = project.GetDocument(document.Id);
		Assert.NotNull(updatedDocument);

		var diagnostics = await GetSortedDiagnosticsFromDocumentsAsync(context, GetCSharpDiagnosticAnalyzer(), [updatedDocument]);
		Assert.Empty(diagnostics);
	}

	private static string CreateSource(string statement, string parameters, string textType = "TMP_Text")
	{
		return $@"
using System.Text;
using TMPro;

class Example
{{
    void UpdateText({textType} label, {parameters})
    {{
        {statement}
    }}
}}
";
	}
}
