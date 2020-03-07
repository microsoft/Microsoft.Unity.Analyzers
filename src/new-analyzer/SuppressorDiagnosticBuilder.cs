﻿/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

namespace NewAnalyzer
{
	internal class SuppressorDiagnosticBuilder : AbstractDiagnosticBuilder
	{
		protected override string IdPrefix => "USP";
		
		protected override string GetResourceTemplate()
		{
			return @"  <data name=""$(DiagnosticName)Justification"" xml:space=""preserve"">
    <value>TODO: Add suppressor justification</value>
  </data>
</root>";
		}

		protected override string GetCodeTemplate()
		{
			return @"/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class $(DiagnosticName) : DiagnosticSuppressor
	{
		internal static readonly SuppressionDescriptor Rule = new SuppressionDescriptor(
			id: ""$(DiagnosticId)"",
			suppressedDiagnosticId: _FIXME_,
			justification: Strings.$(DiagnosticName)Justification);

		public override void ReportSuppressions(SuppressionAnalysisContext context)
		{
			foreach (var diagnostic in context.ReportedDiagnostics)
			{
				AnalyzeDiagnostic(diagnostic, context);
			}
		}

		public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(Rule);

		private static void AnalyzeDiagnostic(Diagnostic diagnostic, SuppressionAnalysisContext context)
		{
			// var sourceTree = diagnostic.Location.SourceTree;
			// var root = sourceTree.GetRoot(context.CancellationToken);
			// var node = root.FindNode(diagnostic.Location.SourceSpan);

			// TODO: context.ReportSuppression
			// example: context.ReportSuppression(Suppression.Create(Rule, diagnostic));
		}
	}
}
";
		}

		protected override string GetTestTemplate()
		{
			return @"/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class $(DiagnosticName)Tests : BaseSuppressorVerifierTest<UnusedMethodSuppressor>
	{
		[Fact]
		public void Test()
		{
			const string test = @""
using UnityEngine;

class Camera : MonoBehaviour
{
}
"";

			var suppressor = ExpectSuppressor($(DiagnosticName).Rule);

			VerifyCSharpDiagnostic(test, suppressor);
		}
	}
}
";
		}
	}
}
