﻿/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class UnusedMethodSuppressorTests : BaseSuppressorVerifierTest<UnusedMethodSuppressor>
	{
		[Fact]
		public void UnusedMethodSuppressed()
		{
			const string test = @"
using UnityEngine;

class Camera : MonoBehaviour
{
    void Start()
    {
        Invoke(""InvokeMe"", 10.0f);
    }

    private void InvokeMe()
    {
        Start(); // we do not want to check for IDE0051 for Unity messages.
    }
}";

			var suppressor = ExpectSuppressor(UnusedMethodSuppressor.Rule)
				.WithLocation(11, 18);

			VerifyCSharpDiagnostic(test, suppressor);
		}
	}
}
