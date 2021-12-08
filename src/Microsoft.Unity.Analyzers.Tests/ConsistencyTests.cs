/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Unity.Analyzers.Tests;

public class ConsistencyTests
{
	private static Dictionary<string, List<int>> CollectIds<T>(Func<T, string> reader) where T : class
	{
		var assembly = typeof(DiagnosticCategory).Assembly;

		var analyzers = assembly
			.GetTypes()
			.Where(t => t.GetCustomAttributes(typeof(DiagnosticAnalyzerAttribute), true).Any());

		var lookup = new Dictionary<string, List<int>>();

		foreach (var analyzer in analyzers)
		{
			var rules = analyzer
				.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
				.Where(f => f.FieldType == typeof(T));

			foreach (var fieldInfo in rules)
			{
				var rule = fieldInfo.GetValue(null) as T;
				Assert.NotNull(rule);

				var id = reader(rule);
				var prefix = id[..3];
				var num = int.Parse(id[3..]);

				if (!lookup.TryGetValue(prefix, out _))
					lookup.Add(prefix, new List<int>());

				lookup[prefix].Add(num);
			}
		}

		return lookup;
	}

	private static void CheckLookup(Dictionary<string, List<int>> lookup)
	{
		foreach (var prefix in lookup.Keys)
		{
			var list = lookup[prefix];
			list.Sort();

			var duplicates = list.GroupBy(x => x)
				.Where(g => g.Count() > 1)
				.Select(y => y.Key)
				.ToList();

			Assert.True(!duplicates.Any(), $"{prefix} IDs are not unique: {string.Join(",", duplicates)}");

			var difference = Enumerable
				.Range(1, list.Count)
				.Except(list)
				.ToList();

			Assert.True(!difference.Any(), $"{prefix} IDs are not contiguous: {string.Join(",", difference)}");
		}
	}

	private readonly ITestOutputHelper _output;

	public ConsistencyTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void CheckAnalyzerIds()
	{
		CheckLookup(CollectIds<DiagnosticDescriptor>(d =>
		{
			_output.WriteLine($"Scanning diagnostic {d.Id}: {d.Description}");
			return d.Id;
		}));
	}

	[Fact]
	public void CheckSuppressorIds()
	{
		CheckLookup(CollectIds<SuppressionDescriptor>(d =>
		{
			_output.WriteLine($"Scanning suppressor {d.Id} for {d.SuppressedDiagnosticId}: {d.Justification}");
			return d.Id;
		}));
	}
}
