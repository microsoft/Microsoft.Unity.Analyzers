/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public class CodeStyleSuppressorTests
{
	[Fact]
	public Task TestCodeStyleIgnore()
	{
		/* This suppressor is used to ignore the IDE1006 diagnostic (naming conventions) for methods
		 * that are part of Unity's MonoBehaviour lifecycle methods, such as Start, Update, etc.
		 * It is intended to be used in Unity projects where these method names are standard, even
		 * when using camel case naming conventions.
		 * 
		 * It was manually tested in both VS ans VSCode, but we don't have an automated test for it.
		 * As soon as we expose the suppressor to the test infrastructure, we hit a "NonImplementedExcetion".
		 * I guess this is because the naming analyzer is one of the remaining components that are hosted
		 * in the IDE and not directly in Roslyn. */

		return Task.CompletedTask;
	}
}
