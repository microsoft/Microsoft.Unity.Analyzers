/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;

namespace Microsoft.Unity.Analyzers.Tests
{
	[Flags]
	public enum DiagnosticLocationOptions
	{
		None = 0,
		IgnoreLength = 1
	}
}
