/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Microsoft.Unity.Analyzers.Tests;

internal class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
	public Assembly LoadFromPath(string fullPath)
	{
		return Assembly.LoadFrom(fullPath);
	}

	public void AddDependencyLocation(string fullPath)
	{
	}
}
