/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class NonAllocatingArrayAccessTests : BaseCodeFixVerifierTest<NonAllocatingArrayAccessAnalyzer, NonAllocatingArrayAccessCodeFix>
{
	[Theory]
	[InlineData("Collision", "value.contacts.Length", "value.contactCount", "contacts", "contactCount")]
	[InlineData("Collision2D", "value.contacts.Length", "value.contactCount", "contacts", "contactCount")]
	[InlineData("object", "Input.touches.Length", "Input.touchCount", "touches", "touchCount")]
	[InlineData("object", "Input.accelerationEvents.Length", "Input.accelerationEventCount", "accelerationEvents", "accelerationEventCount")]
	[InlineData("Collision", "value.contacts[i]", "value.GetContact(i)", "contacts", "GetContact")]
	[InlineData("Collision2D", "value.contacts[i]", "value.GetContact(i)", "contacts", "GetContact")]
	[InlineData("object", "Input.touches[i]", "Input.GetTouch(i)", "touches", "GetTouch")]
	[InlineData("object", "Input.accelerationEvents[i]", "Input.GetAccelerationEvent(i)", "accelerationEvents", "GetAccelerationEvent")]
	[InlineData("Collision", "value.contacts[0].normal", "value.GetContact(0).normal", "contacts", "GetContact")]
	[InlineData("Collision2D", "value.contacts[0].normal", "value.GetContact(0).normal", "contacts", "GetContact")]
	[InlineData("object", "Input.touches[0].position", "Input.GetTouch(0).position", "touches", "GetTouch")]
	[InlineData("object", "Input.accelerationEvents[0].acceleration", "Input.GetAccelerationEvent(0).acceleration", "accelerationEvents", "GetAccelerationEvent")]
	[InlineData("Collision", "value.contacts[i++]", "value.GetContact(i++)", "contacts", "GetContact")]
	[InlineData("Collision2D", "value.contacts[i++]", "value.GetContact(i++)", "contacts", "GetContact")]
	[InlineData("object", "Input.touches[i++]", "Input.GetTouch(i++)", "touches", "GetTouch")]
	[InlineData("object", "Input.accelerationEvents[i++]", "Input.GetAccelerationEvent(i++)", "accelerationEvents", "GetAccelerationEvent")]
	public async Task NonAllocatingRead(string type, string expression, string fixedExpression, string property, string replacement)
	{
		var test = Source(type, $"Debug.Log({expression});");
		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19)
			.WithArguments(replacement, property);

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
		await VerifyCSharpFixAsync(test, Source(type, $"Debug.Log({fixedExpression});"));
	}

	[Theory]
	[InlineData("InputAlias.touches.Length", "InputAlias.touchCount", "touchCount")]
	[InlineData("InputAlias.touches[i]", "InputAlias.GetTouch(i)", "GetTouch")]
	public async Task AliasedType(string expression, string fixedExpression, string replacement)
	{
		const string alias = "using InputAlias = UnityEngine.Input;";
		var test = Source("object", $"Debug.Log({expression});", alias);
		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19)
			.WithArguments(replacement, "touches");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
		await VerifyCSharpFixAsync(test, Source("object", $"Debug.Log({fixedExpression});", alias));
	}

	[Theory]
	[InlineData("value./* property */contacts[i /* index */]", "value./* property */GetContact(i /* index */)", "GetContact")]
	[InlineData("value.contacts /* array */.Length", "value.contactCount /* array */", "contactCount")]
	public async Task PreserveTrivia(string expression, string fixedExpression, string replacement)
	{
		var test = Source("Collision", $"Debug.Log({expression});");
		var diagnostic = ExpectDiagnostic()
			.WithLocation(8, 19)
			.WithArguments(replacement, "contacts");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);
		await VerifyCSharpFixAsync(test, Source("Collision", $"Debug.Log({fixedExpression});"));
	}

	[Theory]
	[InlineData("Collision", "Debug.Log(value.contactCount);")]
	[InlineData("Collision", "Debug.Log(value.GetContact(i));")]
	[InlineData("Collision2D", "Debug.Log(value.contactCount);")]
	[InlineData("Collision2D", "Debug.Log(value.GetContact(i));")]
	[InlineData("object", "Debug.Log(Input.touchCount);")]
	[InlineData("object", "Debug.Log(Input.GetTouch(i));")]
	[InlineData("object", "Debug.Log(Input.accelerationEventCount);")]
	[InlineData("object", "Debug.Log(Input.GetAccelerationEvent(i));")]
	[InlineData("Collision", "Debug.Log(value.contacts);")]
	[InlineData("object", "Debug.Log(Input.touches);")]
	[InlineData("object", "Debug.Log(Input.accelerationEvents);")]
	[InlineData("Collision", "foreach (var contact in value.contacts) Debug.Log(contact);")]
	[InlineData("object", "foreach (var touch in Input.touches) Debug.Log(touch);")]
	[InlineData("object", "foreach (var accelerationEvent in Input.accelerationEvents) Debug.Log(accelerationEvent);")]
	[InlineData("Collision", "Debug.Log(value.contacts.LongLength);")]
	[InlineData("Collision", "Debug.Log(nameof(value.contacts.Length));")]
	[InlineData("object", "Debug.Log(nameof(Input.touches.Length));")]
	[InlineData("ContactPoint[]", "Debug.Log(value.Length);")]
	[InlineData("Touch[]", "Debug.Log(value[i]);")]
	public async Task OtherUsage(string type, string statement)
	{
		await VerifyCSharpDiagnosticAsync(Source(type, statement));
	}

	[Theory]
	[InlineData("Collision", "value.contacts[i] = default;")]
	[InlineData("Collision", "(value.contacts[i], i) = (default, 0);")]
	[InlineData("Collision", "ref var contact = ref value.contacts[i]; Debug.Log(contact);")]
	[InlineData("Collision", "void Modify(ref ContactPoint p) { } Modify(ref value.contacts[i]);")]
	[InlineData("Collision2D", "void Modify(out ContactPoint2D p) { p = default; } Modify(out value.contacts[i]);")]
	[InlineData("object", "void Inspect(in Touch touch) { } Inspect(in Input.touches[i]);")]
	[InlineData("Collision", "void Inspect(in ContactPoint p) { } Inspect(value.contacts[i]);")]
	[InlineData("Collision2D", "void Inspect(in ContactPoint2D p) { } Inspect(value.contacts[i]);")]
	[InlineData("object", "void Inspect(in Touch touch) { } Inspect((Input.touches[i]));")]
	[InlineData("Collision", "Debug.Log(value.contacts[i].ToString());")]
	[InlineData("object", "Input.touches[i].position = Vector2.zero;")]
	[InlineData("object", "Input.accelerationEvents[i] = default;")]
	[InlineData("object", "void Modify(ref AccelerationEvent accelerationEvent) { } Modify(ref Input.accelerationEvents[i]);")]
	[InlineData("object", "Input.touches[i]!.position = Vector2.zero;")]
	[InlineData("Collision", "ref var contact = ref value.contacts[i]!; Debug.Log(contact);")]
	[InlineData("object", "Input.touches[i].tapCount++;")]
	[InlineData("object", "Input.touches[i].tapCount += 1;")]
	[InlineData("Collision", "Debug.Log(value.contacts[(long)i]);")]
	[InlineData("Collision2D", "Debug.Log(value.contacts[(uint)i]);")]
	[InlineData("object", "Debug.Log(Input.touches[(long)i]);")]
	[InlineData("object", "Debug.Log(Input.accelerationEvents[(long)i]);")]
	public async Task NotAValueReadWithIntIndex(string type, string statement)
	{
		await VerifyCSharpDiagnosticAsync(Source(type, statement));
	}

	[Fact]
	public async Task UnrelatedProperty()
	{
		var test = Source("Other", "Debug.Log(value.contacts.Length); Debug.Log(value.contacts[i]);") + @"
class Other
{
    public int[] contacts => new int[1];
    public int contactCount => 1;
    public int GetContact(int index) => 0;
}";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Theory]
	[InlineData("value.contacts.Length")]
	[InlineData("value.contacts[i]")]
	public async Task HiddenReplacement(string expression)
	{
		var test = Source("DerivedCollision", $"Debug.Log({expression});") + @"
class DerivedCollision : Collision
{
    public new int contactCount => 99;
    public new ContactPoint GetContact(int index) => default;
}";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task MultipleReads()
	{
		var test = Source("Collision", "Debug.Log(value.contacts.Length + value.contacts[i].point.x);");
		var fixedTest = Source("Collision", "Debug.Log(value.contactCount + value.GetContact(i).point.x);");

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task ReceiverEvaluatedOnce()
	{
		var test = Source("Collision", "Collision GetCollision() => value; Debug.Log(GetCollision().contacts[i]);");
		var fixedTest = Source("Collision", "Collision GetCollision() => value; Debug.Log(GetCollision().GetContact(i));");

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task AwaitedIndex()
	{
		const string test = @"
using UnityEngine;
using System.Threading.Tasks;

class Example
{
    async Task<ContactPoint> Read(Collision collision, Task<int> index)
    {
        return collision.contacts[await index];
    }
}";

		await VerifyCSharpDiagnosticAsync(test);
	}

	private static string Source(string type, string statement, string extraUsing = "")
	{
		return $@"
using UnityEngine;
{extraUsing}
class Example : MonoBehaviour
{{
    void Method({type} value, int i)
    {{
        {statement}
    }}
}}";
	}
}
