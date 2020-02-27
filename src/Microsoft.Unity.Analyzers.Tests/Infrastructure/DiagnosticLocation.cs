/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Microsoft.CodeAnalysis;

namespace Microsoft.Unity.Analyzers.Tests
{
	public struct DiagnosticLocation
	{
		public DiagnosticLocation(FileLinePositionSpan span, DiagnosticLocationOptions options)
		{
			Span = span;
			Options = options;
		}

		public FileLinePositionSpan Span { get; }

		public DiagnosticLocationOptions Options { get; }
	}
}
