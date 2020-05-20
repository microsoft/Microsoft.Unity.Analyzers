/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	public class SetPixelsMethodUsageAttribute : MethodUsageAttribute
	{
	}

	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class SetPixelsMethodUsageAnalyzer : BaseMethodUsageAnalyzer<SetPixelsMethodUsageAttribute> 
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0017",
			title: Strings.SetPixelsMethodUsageDiagnosticTitle,
			messageFormat: Strings.SetPixelsMethodUsageDiagnosticMessageFormat,
			category: DiagnosticCategory.Performance,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.SetPixelsMethodUsageDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
	}
}
