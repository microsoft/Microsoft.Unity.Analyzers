/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class InputSystemMessageSuppressorTests : BaseSuppressorVerifierTest<InputSystemMessageSuppressor>
{
	// The Input System package is not available in test infrastructure, so tests declare matching types.

	[Fact]
	public async Task InputValueMessageSuppressed()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEngine.InputSystem
{
    public class InputValue
    {
        public T Get<T>() { return default(T); }
    }
}

class Player : MonoBehaviour
{
    public Vector2 rawInput;

    private void OnMove(InputValue value)
    {
        rawInput = value.Get<Vector2>();
    }
}";

		var suppressor = ExpectSuppressor(InputSystemMessageSuppressor.MethodRule)
			.WithLocation(17, 18);

		await VerifyCSharpDiagnosticAsync(test, suppressor);
	}

	[Fact]
	public async Task CallbackContextMessageAndParameterSuppressed()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEngine.InputSystem
{
    public class InputAction
    {
        public struct CallbackContext { }
    }
}

class Player : MonoBehaviour
{
    private void OnFire(InputAction.CallbackContext context)
    {
    }
}";

		var suppressors = new[] {
			ExpectSuppressor(InputSystemMessageSuppressor.MethodRule).WithLocation(15, 18),
			ExpectSuppressor(InputSystemMessageSuppressor.ParameterRule).WithLocation(15, 53),
		};

		await VerifyCSharpDiagnosticAsync(test, suppressors);
	}

	[Fact]
	public async Task PlayerInputNotificationMessageAndParameterSuppressed()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEngine.InputSystem
{
    public class PlayerInput { }
}

class Player : MonoBehaviour
{
    private void OnDeviceLost(PlayerInput input)
    {
    }
}";

		var suppressors = new[] {
			ExpectSuppressor(InputSystemMessageSuppressor.MethodRule).WithLocation(12, 18),
			ExpectSuppressor(InputSystemMessageSuppressor.ParameterRule).WithLocation(12, 43),
		};

		await VerifyCSharpDiagnosticAsync(test, suppressors);
	}

	[Fact]
	public async Task MethodWithoutOnPrefixNotSuppressed()
	{
		const string test = @"
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEngine.InputSystem
{
    public class InputValue
    {
        public T Get<T>() { return default(T); }
    }
}

class Player : MonoBehaviour
{
    public Vector2 rawInput;

    private void HandleMove(InputValue value)
    {
        rawInput = value.Get<Vector2>();
    }
}";

		var not = ExpectNotSuppressed(InputSystemMessageSuppressor.MethodRule)
			.WithLocation(17, 18)
			.WithSeverity(DiagnosticSeverity.Info)
			.WithMessageFormat("Private member '{0}' is unused")
			.WithArguments("Player.HandleMove");

		await VerifyCSharpDiagnosticAsync(test, not);
	}
}
