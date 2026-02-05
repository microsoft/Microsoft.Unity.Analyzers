/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class AnimatorStringToHashTests : BaseCodeFixVerifierTest<AnimatorStringToHashAnalyzer, AnimatorStringToHashCodeFix>
{
	[Fact]
	public async Task AnimatorPlayWithStringLiteral() // stateName vs stateNameHash
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private Animator _animator = null;

    void Start()
    {
        _animator.Play(""Attack"");
    }
}
";

		var diagnostic = ExpectDiagnostic()
				.WithLocation(10, 9)
				.WithArguments("Play", "Attack");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private static readonly int AttackHash = Animator.StringToHash(""Attack"");
    private Animator _animator = null;

    void Start()
    {
        _animator.Play(AttackHash);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task AnimatorPlayWithVariable_NoDiagnostic()
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private Animator _animator = null;
    private string _stateName = ""Attack"";

    void Start()
    {
        _animator.Play(_stateName);
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task NonAnimatorType_NoDiagnostic()
	{
		const string test = @"
using UnityEngine;

class OtherClass
{
    public void Play(string name) { }
}

class Test : MonoBehaviour
{
    private OtherClass _other = null;

    void Start()
    {
        _other.Play(""Attack"");
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task AnimatorSetBoolWithStringLiteral() // name vs id
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private Animator _animator = null;

    void Update()
    {
        _animator.SetBool(""IsRunning"", true);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 9)
			.WithArguments("SetBool", "IsRunning");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private static readonly int IsRunningHash = Animator.StringToHash(""IsRunning"");
    private Animator _animator = null;

    void Update()
    {
        _animator.SetBool(IsRunningHash, true);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task AnimatorSetTriggerWithStringLiteral()
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private Animator _animator = null;

    void OnTriggerEnter(Collider other)
    {
        _animator.SetTrigger(""Jump"");
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 9)
			.WithArguments("SetTrigger", "Jump");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private static readonly int JumpHash = Animator.StringToHash(""Jump"");
    private Animator _animator = null;

    void OnTriggerEnter(Collider other)
    {
        _animator.SetTrigger(JumpHash);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task AnimatorGetFloatWithStringLiteral()
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private Animator _animator = null;

    void Update()
    {
        float speed = _animator.GetFloat(""Speed"");
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 23)
			.WithArguments("GetFloat", "Speed");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash(""Speed"");
    private Animator _animator = null;

    void Update()
    {
        float speed = _animator.GetFloat(SpeedHash);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}


	[Fact]
	public async Task AnimatorCrossFadeWithStringLiteral() // stateName vs stateNameHash
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private Animator _animator = null;

    void Start()
    {
        _animator.CrossFade(""Idle"", 0.5f);
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 9)
			.WithArguments("CrossFade", "Idle");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private static readonly int IdleHash = Animator.StringToHash(""Idle"");
    private Animator _animator = null;

    void Start()
    {
        _animator.CrossFade(IdleHash, 0.5f);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task AnimatorWithAlreadyHashedCall_NoDiagnostic()
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private static readonly int AttackHash = Animator.StringToHash(""Attack"");
    private Animator _animator = null;

    void Start()
    {
        _animator.Play(AttackHash);
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task AnimatorWithExistingHashField()
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private static readonly int AttackHash = Animator.StringToHash(""Attack"");
    private Animator _animator = null;

    void Start()
    {
        _animator.Play(""Attack"");
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(11, 9)
			.WithArguments("Play", "Attack");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private static readonly int AttackHash = Animator.StringToHash(""Attack"");
    private Animator _animator = null;

    void Start()
    {
        _animator.Play(AttackHash);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task AnimatorWithConstString_NoDiagnostic()
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private const string AttackState = ""Attack"";
    private Animator _animator = null;

    void Start()
    {
        _animator.Play(AttackState);
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task AnimatorWithMethodParameter_NoDiagnostic()
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private Animator _animator = null;

    void PlayAnimation(string stateName)
    {
        _animator.Play(stateName);
    }
}
";

		await VerifyCSharpDiagnosticAsync(test);
	}

	[Fact]
	public async Task AnimatorWithSpecialCharactersInStateName()
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private Animator _animator = null;

    void Start()
    {
        _animator.Play(""Attack-Heavy 01"");
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(10, 9)
			.WithArguments("Play", "Attack-Heavy 01");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private static readonly int AttackHeavy01Hash = Animator.StringToHash(""Attack-Heavy 01"");
    private Animator _animator = null;

    void Start()
    {
        _animator.Play(AttackHeavy01Hash);
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}

	[Fact]
	public async Task AnimatorMultipleCalls()
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private Animator _animator = null;

    void Start()
    {
        _animator.Play(""Attack"");
        _animator.SetBool(""IsRunning"", true);
    }
}
";

		var diagnostic1 = ExpectDiagnostic()
			.WithLocation(10, 9)
			.WithArguments("Play", "Attack");

		var diagnostic2 = ExpectDiagnostic()
			.WithLocation(11, 9)
			.WithArguments("SetBool", "IsRunning");

		await VerifyCSharpDiagnosticAsync(test, diagnostic1, diagnostic2);
	}

	[Fact]
	public async Task AnimatorPlayWithStringLiteralTrivia()
	{
		const string test = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private Animator _animator = null;

    void Start()
    {
        // comment before
        _animator.Play(/* inline before */ ""Attack"" /* inline after */);
        // comment after
    }
}
";

		var diagnostic = ExpectDiagnostic()
			.WithLocation(11, 9)
			.WithArguments("Play", "Attack");

		await VerifyCSharpDiagnosticAsync(test, diagnostic);

		const string fixedTest = @"
using UnityEngine;

class Test : MonoBehaviour
{
    private static readonly int AttackHash = Animator.StringToHash(""Attack"");
    private Animator _animator = null;

    void Start()
    {
        // comment before
        _animator.Play(/* inline before */ AttackHash /* inline after */);
        // comment after
    }
}
";

		await VerifyCSharpFixAsync(test, fixedTest);
	}
}
