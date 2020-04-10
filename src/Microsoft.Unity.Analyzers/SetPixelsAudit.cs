/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	public class SetPixelsAuditAttribute : AuditAttribute
	{
	}

	public class SetPixelsAuditAnalyzer : BaseAuditAnalyzer<SetPixelsAuditAttribute> 
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: "UNT0015",
			title: Strings.SetPixelsAuditDiagnosticTitle,
			messageFormat: Strings.SetPixelsAuditDiagnosticMessageFormat,
			category: DiagnosticCategory.Performance,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.SetPixelsAuditDiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
	}
}
