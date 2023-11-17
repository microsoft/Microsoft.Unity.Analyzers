/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Microsoft.CodeAnalysis;

namespace Microsoft.Unity.Analyzers.Tests;

public readonly struct DiagnosticLocation(FileLinePositionSpan span, DiagnosticLocationOptions options)
{
	public FileLinePositionSpan Span { get; } = span;

	public DiagnosticLocationOptions Options { get; } = options;
}
