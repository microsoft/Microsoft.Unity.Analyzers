/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;

namespace NewAnalyzer;

internal class Program
{
	private static void Main(string[] args)
	{
		string? name;
		if (args.Length == 0)
		{
			Console.Write("Diagnostic name: ");
			name = Console.ReadLine();
		}
		else
		{
			name = args[0];
		}

		var builder = !string.IsNullOrEmpty(name) && name.Contains("suppressor", StringComparison.OrdinalIgnoreCase) ?
			new SuppressorDiagnosticBuilder() as AbstractDiagnosticBuilder :
			new AnalyzerCodeFixDiagnosticBuilder();

		builder.Build(name ?? "Unknown");
	}
}
