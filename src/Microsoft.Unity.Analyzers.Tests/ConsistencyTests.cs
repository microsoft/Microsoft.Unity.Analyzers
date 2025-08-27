/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Unity.Analyzers.Tests;

public class ConsistencyTests(ITestOutputHelper output)
{
	private static Dictionary<string, List<int>> CollectIds<T>(Func<T, string> reader) where T : class
	{
		var lookup = new Dictionary<string, List<int>>();

		foreach (var rule in CollectRules<T>())
		{
			var id = reader(rule);
			var prefix = id[..3];
			var num = int.Parse(id[3..]);

			if (!lookup.TryGetValue(prefix, out _))
				lookup.Add(prefix, []);

			lookup[prefix].Add(num);
		}

		return lookup;
	}

	private static List<T> CollectRules<T>() where T : class
	{
		var assembly = typeof(DiagnosticCategory).Assembly;

		var analyzers = assembly
			.GetTypes()
			.Where(t => t.GetCustomAttributes(typeof(DiagnosticAnalyzerAttribute), true).Any());

		List<T> rules = [];

		foreach (var analyzer in analyzers)
		{
			var fields = analyzer
				.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
				.Where(f => f.FieldType == typeof(T))
				.ToArray();

			var localRules = fields
				.Select(f => f.GetValue(null))
				.OfType<T>()
				.ToArray();

			rules.AddRange(localRules);

			if (fields.Length != 0)
				Assert.NotEmpty(localRules);
		}

		return rules;
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

			Assert.False(duplicates.Any(), $"{prefix} IDs are not unique: {string.Join(",", duplicates)}");

			var difference = Enumerable
				.Range(1, list.Count)
				.Except(list)
				.ToList();

			Assert.False(difference.Any(), $"{prefix} IDs are not contiguous: {string.Join(",", difference)}");
		}
	}

	[Fact]
	public void CheckAnalyzerIds()
	{
		CheckLookup(CollectIds<DiagnosticDescriptor>(d =>
		{
			output.WriteLine($"Scanning diagnostic {d.Id}: {d.Description}");
			return d.Id;
		}));
	}

	[Fact]
	public void CheckSuppressorIds()
	{
		CheckLookup(CollectIds<SuppressionDescriptor>(d =>
		{
			output.WriteLine($"Scanning suppressor {d.Id} for {d.SuppressedDiagnosticId}: {d.Justification}");
			return d.Id;
		}));
	}

	[Fact]
	public async Task CheckHelpLinks()
	{

		var rules = CollectRules<DiagnosticDescriptor>();

		using var client = new HttpClient();
		foreach (var d in rules)
		{
			output.WriteLine($"Scanning diagnostic {d.Id}: {d.Description}");
			Assert.NotEmpty(d.HelpLinkUri);
			Assert.Contains(d.Id, d.HelpLinkUri);

			var response = await client.GetAsync(d.HelpLinkUri);
			Assert.True(response.IsSuccessStatusCode);
		}
	}

}
