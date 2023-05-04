/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

namespace Microsoft.Unity.Analyzers;

internal class HelpLink
{
	public static string ForDiagnosticId(string ruleId)
	{
		return $"https://github.com/microsoft/Microsoft.Unity.Analyzers/blob/main/doc/{ruleId}.md";
	}
}
