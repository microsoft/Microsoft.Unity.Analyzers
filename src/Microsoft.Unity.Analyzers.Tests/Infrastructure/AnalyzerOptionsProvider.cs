/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Unity.Analyzers.Tests
{
	internal class AnalyzerOptionsProvider : AnalyzerConfigOptionsProvider
	{
		private readonly AnalyzerConfigOptionsLookup _lookup;

		public AnalyzerOptionsProvider(IDictionary<string, string> overrides)
		{
			_lookup = new AnalyzerConfigOptionsLookup(overrides);
		}

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
		{
			return _lookup;
		}

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
		{
			return _lookup;
		}

		public override AnalyzerConfigOptions GlobalOptions
		{
			get
			{
				return _lookup;
			}
		}
	}

	internal class AnalyzerConfigOptionsLookup : AnalyzerConfigOptions
	{
		private readonly IDictionary<string, string> _default = new Dictionary<string, string>();
		private readonly IDictionary<string, string> _overrides;

		public AnalyzerConfigOptionsLookup(IDictionary<string, string> overrides)
		{
			_overrides = overrides;
			_default.Add("generated_code", "false");

			_default.Add("dotnet_analyzer_diagnostic.severity", "info");
			_default.Add("dotnet_analyzer_diagnostic.category-CodeQuality.severity", "info");
			_default.Add("dotnet_analyzer_diagnostic.category-Correctness.severity", "info");
			_default.Add("dotnet_analyzer_diagnostic.category-Performance.severity", "info");
			_default.Add("dotnet_analyzer_diagnostic.category-Style.severity", "info");
			_default.Add("dotnet_analyzer_diagnostic.category-Type Safety.severity", "info");
			_default.Add("dotnet_analyzer_diagnostic.category-Usage.severity", "info");

			_default.Add("dotnet_style_coalesce_expression", "true");
			_default.Add("dotnet_style_null_propagation", "true");
			_default.Add("dotnet_style_readonly_field", "true:suggestion");
			_default.Add("dotnet_style_prefer_compound_assignment", "true");

			_default.Add("csharp_style_unused_value_expression_statement_preference", "discard_variable");
			_default.Add("csharp_style_unused_value_assignment_preference", "discard_variable");

			_default.Add("dotnet_code_quality_unused_parameters", "all");

			_default.Add("build_property.UsingMicrosoftNETSdkWeb", "false");
			_default.Add("build_property.ProjectTypeGuids", "");

			_default.Add("dotnet_code_quality.CA1801.api_surface", "");
			_default.Add("dotnet_code_quality.CA1822.api_surface", "");
			_default.Add("dotnet_code_quality.Performance.api_surface", "");
			_default.Add("dotnet_code_quality.PortedFromFxCop.api_surface", "");
			_default.Add("dotnet_code_quality.Telemetry.api_surface", "");
			_default.Add("dotnet_code_quality.EnabledRuleInAggressiveMode.api_surface", "");
			_default.Add("dotnet_code_quality.api_surface", "");
			_default.Add("dotnet_code_quality.Usage.api_surface", "");
		}

		public override bool TryGetValue(string key, out string value)
		{
			if (_overrides.TryGetValue(key, out value))
				return true;

			if (_default.TryGetValue(key, out value))
				return true;

			throw new ArgumentException($"Unexpected analyzer option requested '{key}'");
		}
	}
}
