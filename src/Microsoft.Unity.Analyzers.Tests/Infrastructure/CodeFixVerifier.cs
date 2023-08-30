/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

public abstract class CodeFixVerifier : DiagnosticVerifier
{
	protected abstract CodeFixProvider GetCSharpCodeFixProvider();

	protected Task VerifyCSharpFixAsync(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false)
	{
		return VerifyCSharpFixAsync(AnalyzerVerificationContext.Default, oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics);
	}

	protected Task VerifyCSharpFixAsync(AnalyzerVerificationContext context, string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false)
	{
		return VerifyFixAsync(context, GetCSharpDiagnosticAnalyzer(), GetCSharpCodeFixProvider(), oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics);
	}

	private async Task VerifyFixAsync(AnalyzerVerificationContext context, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string oldSource, string newSource, int? codeFixIndex, bool allowNewCompilerDiagnostics)
	{
		var document = CreateDocument(context, oldSource);
		var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(context, analyzer, new[] {document});
		var compilerDiagnostics = (await GetCompilerDiagnosticsAsync(document)).ToList();
		var attempts = analyzerDiagnostics.Length;

		for (var i = 0; i < attempts; ++i)
		{
			var actions = new List<CodeAction>();
			var codeFixContext = new CodeFixContext(document, analyzerDiagnostics[0], (a, _) => actions.Add(a), CancellationToken.None);
			await codeFixProvider.RegisterCodeFixesAsync(codeFixContext);

			if (!actions.Any())
			{
				break;
			}

			if (codeFixIndex != null)
			{
				document = await ApplyFixAsync(document, actions.ElementAt((int)codeFixIndex));
				break;
			}

			document = await ApplyFixAsync(document, actions.ElementAt(0));
			analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(context, analyzer, new[] {document});

			var newCompilerDiagnostics = GetNewDiagnostics(context, compilerDiagnostics, await GetCompilerDiagnosticsAsync(document));

			//check if applying the code fix introduced any new compiler diagnostics
			if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
			{
				// Format and get the compiler diagnostics again so that the locations make sense in the output
				var root = await document.GetSyntaxRootAsync();
				Assert.NotNull(root);

				document = document.WithSyntaxRoot(Formatter.Format(root, Formatter.Annotation, document.Project.Solution.Workspace));
				newCompilerDiagnostics = GetNewDiagnostics(context, compilerDiagnostics, await GetCompilerDiagnosticsAsync(document));

				var diagnostics = string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString()));

				var newDoc = root.ToFullString();
				Assert.Fail($"Fix introduced new compiler diagnostics:\r\n{diagnostics}\r\n\r\nNew document:\r\n{newDoc}\r\n");
			}

			//check if there are analyzer diagnostics left after the code fix
			if (!analyzerDiagnostics.Any())
			{
				break;
			}
		}

		//after applying all of the code fixes, compare the resulting string to the inputted one
		var actual = await GetStringFromDocumentAsync(document);
		Assert.Equal(newSource, actual);
	}

	private static async Task<Document> ApplyFixAsync(Document document, CodeAction codeAction)
	{
		var operations = await codeAction.GetOperationsAsync(CancellationToken.None);
		var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;

		Assert.NotNull(solution);
		return solution.GetDocument(document.Id)!;
	}

	private static IEnumerable<Diagnostic> GetNewDiagnostics(AnalyzerVerificationContext context, IEnumerable<Diagnostic> diagnostics, IEnumerable<Diagnostic> newDiagnostics)
	{
		var oldArray = FilterDiagnostics(diagnostics, context.Filters).OrderBy(d => d.Location.SourceSpan.Start).ToArray();
		var newArray = FilterDiagnostics(newDiagnostics, context.Filters).OrderBy(d => d.Location.SourceSpan.Start).ToArray();

		var oldIndex = 0;
		var newIndex = 0;

		while (newIndex < newArray.Length)
		{
			if (oldIndex < oldArray.Length && oldArray[oldIndex].Id == newArray[newIndex].Id)
			{
				++oldIndex;
				++newIndex;
			}
			else
			{
				yield return newArray[newIndex++];
			}
		}
	}

	private static async Task<IEnumerable<Diagnostic>> GetCompilerDiagnosticsAsync(Document document)
	{
		var model = await document.GetSemanticModelAsync();
		Assert.NotNull(model);

		return model.GetDiagnostics();
	}

	private static async Task<string> GetStringFromDocumentAsync(Document document)
	{
		var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation);
		var root = await simplifiedDoc.GetSyntaxRootAsync();
		Assert.NotNull(root);

		root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);
		return root.GetText().ToString();
	}
}
