/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

namespace NewAnalyzer
{
	internal class AnalyzerCodeFixDiagnosticBuilder : AbstractDiagnosticBuilder
	{
		protected override string IdPrefix => "UNT";
		
		protected override string GetResourceTemplate()
		{
			return @"  <data name=""$(DiagnosticName)CodeFixTitle"" xml:space=""preserve"">
    <value>TODO: Add code fix title</value>
  </data>
  <data name=""$(DiagnosticName)DiagnosticDescription"" xml:space=""preserve"">
    <value>TODO: Add description.</value>
  </data>
  <data name=""$(DiagnosticName)DiagnosticMessageFormat"" xml:space=""preserve"">
    <value>TODO: Add Message format</value>
  </data>
  <data name=""$(DiagnosticName)DiagnosticTitle"" xml:space=""preserve"">
    <value>TODO: Add title</value>
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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class $(DiagnosticName)Analyzer : DiagnosticAnalyzer
	{
		internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			id: ""$(DiagnosticId)"",
			title: Strings.$(DiagnosticName)DiagnosticTitle,
			messageFormat: Strings.$(DiagnosticName)DiagnosticMessageFormat,
			category: DiagnosticCategory._FIXME_,
			defaultSeverity: DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Strings.$(DiagnosticName)DiagnosticDescription);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			
			// TODO: context.RegisterSyntaxNodeAction
			// example: context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
		}
	}

	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public class $(DiagnosticName)CodeFix : CodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create($(DiagnosticName)Analyzer.Rule.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			// var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			// var declaration = root.FindNode(context.Span) as MethodDeclarationSyntax;
			// if (declaration == null)
			//     return;

			// context.RegisterCodeFix(
			//     CodeAction.Create(
			//         Strings.$(DiagnosticName)CodeFixTitle,
			//         ct => {},
			//         declaration.ToFullString()),
			//     context.Diagnostics);
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
	public class $(DiagnosticName)Tests : BaseCodeFixVerifierTest<$(DiagnosticName)Analyzer, $(DiagnosticName)CodeFix>
	{
		[Fact]
		public async void Test()
		{
			const string test = @""
using UnityEngine;

class Camera : MonoBehaviour
{
}
"";

			var diagnostic = ExpectDiagnostic();

			await VerifyCSharpDiagnosticAsync(test, diagnostic);

			const string fixedTest = @""
using UnityEngine;

class Camera : MonoBehaviour
{
}
"";

			await VerifyCSharpFixAsync(test, fixedTest);
		}
	}
}
";
		}
	}
}
