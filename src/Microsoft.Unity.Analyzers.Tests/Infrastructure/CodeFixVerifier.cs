/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public abstract class CodeFixVerifier : DiagnosticVerifier
	{
		protected abstract CodeFixProvider GetCSharpCodeFixProvider();

		protected void VerifyCSharpFix(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false)
		{
			VerifyFix(GetCSharpDiagnosticAnalyzer(), GetCSharpCodeFixProvider(), oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics);
		}

		private void VerifyFix(DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string oldSource, string newSource, int? codeFixIndex, bool allowNewCompilerDiagnostics)
		{
			var document = CreateDocument(oldSource);
			var analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });
			var compilerDiagnostics = GetCompilerDiagnostics(document);
			var attempts = analyzerDiagnostics.Length;

			for (var i = 0; i < attempts; ++i)
			{
				var actions = new List<CodeAction>();
				var context = new CodeFixContext(document, analyzerDiagnostics[0], (a, d) => actions.Add(a), CancellationToken.None);
				codeFixProvider.RegisterCodeFixesAsync(context).Wait();

				if (!actions.Any())
				{
					break;
				}

				if (codeFixIndex != null)
				{
					document = ApplyFix(document, actions.ElementAt((int)codeFixIndex));
					break;
				}

				document = ApplyFix(document, actions.ElementAt(0));
				analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, new[] { document });

				var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, GetCompilerDiagnostics(document));

				//check if applying the code fix introduced any new compiler diagnostics
				if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any(d => d.Id != "CS1701"))
				{
					// Format and get the compiler diagnostics again so that the locations make sense in the output
					document = document.WithSyntaxRoot(Formatter.Format(document.GetSyntaxRootAsync().Result, Formatter.Annotation, document.Project.Solution.Workspace));
					newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, GetCompilerDiagnostics(document));

					var diagnostics = string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString()));
					var newDoc = document.GetSyntaxRootAsync().Result.ToFullString();
					Assert.True(false, $"Fix introduced new compiler diagnostics:\r\n{diagnostics}\r\n\r\nNew document:\r\n{newDoc}\r\n");
				}

				//check if there are analyzer diagnostics left after the code fix
				if (!analyzerDiagnostics.Any())
				{
					break;
				}
			}

			//after applying all of the code fixes, compare the resulting string to the inputted one
			var actual = GetStringFromDocument(document);
			Assert.Equal(newSource, actual);
		}

		private static Document ApplyFix(Document document, CodeAction codeAction)
		{
			var operations = codeAction.GetOperationsAsync(CancellationToken.None).Result;
			var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
			return solution.GetDocument(document.Id);
		}

		private static IEnumerable<Diagnostic> GetNewDiagnostics(IEnumerable<Diagnostic> diagnostics, IEnumerable<Diagnostic> newDiagnostics)
		{
			var oldArray = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
			var newArray = newDiagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();

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

		private static ImmutableArray<Diagnostic> GetCompilerDiagnostics(Document document)
		{
			return document.GetSemanticModelAsync().Result.GetDiagnostics();
		}

		private static string GetStringFromDocument(Document document)
		{
			var simplifiedDoc = Simplifier.ReduceAsync(document, Simplifier.Annotation).Result;
			var root = simplifiedDoc.GetSyntaxRootAsync().Result;
			root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);
			return root.GetText().ToString();
		}
	}
}
