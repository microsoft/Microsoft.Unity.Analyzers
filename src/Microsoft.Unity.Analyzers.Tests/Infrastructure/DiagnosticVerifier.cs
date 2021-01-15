/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	public abstract class DiagnosticVerifier
	{
		private const string DefaultFilePathPrefix = "Test";
		private const string CSharpDefaultFileExt = "cs";
		private const string TestProjectName = "TestProject";

		public virtual LanguageVersion LanguageVersion => LanguageVersion.Latest;

		protected abstract DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer();

		protected virtual IEnumerable<DiagnosticAnalyzer> GetRelatedAnalyzers(DiagnosticAnalyzer analyzer)
		{
			return Enumerable.Empty<DiagnosticAnalyzer>();
		}

		protected static DiagnosticResult ExpectDiagnostic(DiagnosticDescriptor descriptor)
		{
			return new DiagnosticResult(descriptor);
		}

		protected DiagnosticResult ExpectDiagnostic(string diagnosticId)
		{
			var analyzer = GetCSharpDiagnosticAnalyzer();
			try
			{
				return ExpectDiagnostic(analyzer.SupportedDiagnostics.Single(i => i.Id == diagnosticId));
			}
			catch (InvalidOperationException ex)
			{
				throw new InvalidOperationException(
					$"'{nameof(Diagnostic)}(string)' can only be used when the analyzer has a single supported diagnostic with the specified ID. Use the '{nameof(Diagnostic)}(DiagnosticDescriptor)' overload to specify the descriptor from which to create the expected result.",
					ex);
			}
		}

		protected DiagnosticResult ExpectDiagnostic()
		{
			var analyzer = GetCSharpDiagnosticAnalyzer();
			try
			{
				return ExpectDiagnostic(analyzer.SupportedDiagnostics.Single());
			}
			catch (InvalidOperationException ex)
			{
				throw new InvalidOperationException(
					$"'{nameof(Diagnostic)}()' can only be used when the analyzer has a single supported diagnostic. Use the '{nameof(Diagnostic)}(DiagnosticDescriptor)' overload to specify the descriptor from which to create the expected result.",
					ex);
			}
		}

		protected Task VerifyCSharpDiagnosticAsync(string source, params DiagnosticResult[] expected)
		{
			return VerifyDiagnosticsAsync(new[] { source }, GetCSharpDiagnosticAnalyzer(), expected);
		}

		protected Task VerifyCSharpDiagnosticAsync(string[] sources, params DiagnosticResult[] expected)
		{
			return VerifyDiagnosticsAsync(sources, GetCSharpDiagnosticAnalyzer(), expected);
		}

		private async Task VerifyDiagnosticsAsync(string[] sources, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expected)
		{
			var diagnostics = await GetSortedDiagnosticsAsync(sources, analyzer, expected);
			VerifyDiagnosticResults(diagnostics, analyzer, expected);
		}

		protected virtual void VerifyDiagnosticResults(Diagnostic[] actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
		{
			var expectedCount = expectedResults.Length;
			var actualCount = actualResults.Length;

			if (expectedCount != actualCount)
			{
				var diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, actualResults.ToArray()) : "    NONE.";

				Assert.True(false, $"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n");
			}

			for (var i = 0; i < expectedResults.Length; i++)
			{
				var actual = actualResults.ElementAt(i);
				var expected = expectedResults[i];

				if (!expected.HasLocation)
				{
					if (actual.Location != Location.None)
					{
						Assert.True(false, $"Expected:\nA project diagnostic with No location\nActual:\n{FormatDiagnostics(analyzer, actual)}");
					}
				}
				else
				{
					VerifyDiagnosticLocations(analyzer, actual, actual.Location, expected.Spans);
				}

				if (actual.Id != expected.Id)
				{
					Assert.True(false, $"Expected diagnostic id to be \"{expected.Id}\" was \"{actual.Id}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
				}

				if (actual.Severity != expected.Severity)
				{
					Assert.True(false, $"Expected diagnostic severity to be \"{expected.Severity}\" was \"{actual.Severity}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
				}

				if (actual.GetMessage() != expected.Message)
				{
					Assert.True(false, $"Expected diagnostic message to be \"{expected.Message}\" was \"{actual.GetMessage()}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
				}
			}
		}

		private static void VerifyDiagnosticLocations(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, IEnumerable<DiagnosticLocation> locations)
		{
			var actualSpan = actual.GetLineSpan();
			var expected = locations.First();

			var actualLinePosition = actualSpan.StartLinePosition;

			// Only check line position if there is an actual line in the real diagnostic
			if (actualLinePosition.Line > 0)
			{
				if (actualLinePosition.Line != expected.Span.StartLinePosition.Line)
				{
					Assert.True(false, $"Expected diagnostic to be on line \"{expected.Span.StartLinePosition.Line + 1}\" was actually on line \"{actualLinePosition.Line + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");
				}
			}

			// Only check column position if there is an actual column position in the real diagnostic
			if (actualLinePosition.Character > 0)
			{
				if (actualLinePosition.Character != expected.Span.StartLinePosition.Character)
				{
					Assert.True(false, $"Expected diagnostic to start at column \"{expected.Span.StartLinePosition.Character + 1}\" was actually at column \"{actualLinePosition.Character + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");
				}
			}
		}

		private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
		{
			var builder = new StringBuilder();
			for (var i = 0; i < diagnostics.Length; ++i)
			{
				builder.AppendLine("// " + diagnostics[i]);

				var analyzerType = analyzer.GetType();
				var rules = analyzer.SupportedDiagnostics;

				foreach (var rule in rules)
				{
					if (rule.Id != diagnostics[i].Id)
						continue;

					var location = diagnostics[i].Location;
					if (location == Location.None)
					{
						builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
					}
					else
					{
						Assert.True(location.IsInSource, $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

						var syntaxTree = diagnostics[i].Location.SourceTree;
						Assert.NotNull(syntaxTree);

						var resultMethodName = syntaxTree.FilePath.EndsWith(".cs") ? "GetCSharpResultAt" : "GetBasicResultAt";
						var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

						builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
							resultMethodName,
							linePosition.Line + 1,
							linePosition.Character + 1,
							analyzerType.Name,
							rule.Id);
					}

					if (i != diagnostics.Length - 1)
					{
						builder.Append(',');
					}

					builder.AppendLine();
					break;
				}
			}
			return builder.ToString();
		}

		private Task<Diagnostic[]> GetSortedDiagnosticsAsync(string[] sources, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expected)
		{
			return GetSortedDiagnosticsFromDocumentsAsync(analyzer, GetDocuments(sources), expected);
		}

		protected async Task<Diagnostic[]> GetSortedDiagnosticsFromDocumentsAsync(DiagnosticAnalyzer analyzer, Document[] documents, params DiagnosticResult[] expected)
		{
			var projects = new HashSet<Project>();
			foreach (var document in documents)
			{
				projects.Add(document.Project);
			}

			var diagnostics = new List<Diagnostic>();
			foreach (var project in projects)
			{
				var analyzers = ImmutableArray.Create(analyzer)
					.AddRange(GetRelatedAnalyzers(analyzer));

				var compilation = await project.GetCompilationAsync();
				Assert.NotNull(compilation);

				var overrides = expected
					.SelectMany(d => d.Options)
					.GroupBy(d => d.Key)
					.ToImmutableDictionary(kvp => kvp.Key, g => g.First().Value);

				var optionsProvider = new AnalyzerOptionsProvider(overrides);
				var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, optionsProvider);
				var analyzerOptions = new CompilationWithAnalyzersOptions(options, null, true, true, true);

				var compilationWithAnalyzers = compilation
					.WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, reportSuppressedDiagnostics: true))
					.WithAnalyzers(analyzers, analyzerOptions);

				var allDiagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync();
				var errors = allDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
				foreach (var error in errors)
				{
					Assert.True(false, $"Line {error.Location.GetLineSpan().StartLinePosition.Line}: {error.GetMessage()}");
				}

				var diags = allDiagnostics
					.Except(errors)
					.Where(d => d.Location.IsInSource); //only keep diagnostics related to a source location

				foreach (var diag in diags)
				{
					// We should not hit this anymore, but keep in case we change the previous filter
					if (diag.Location == Location.None || diag.Location.IsInMetadata)
					{
						diagnostics.Add(diag);
					}
					else
					{
						foreach (var document in documents)
						{
							var tree = await document.GetSyntaxTreeAsync();
							if (tree == diag.Location.SourceTree)
							{
								diagnostics.Add(diag);
							}
						}
					}
				}
			}

			var results = SortDiagnostics(FilterDiagnostics(diagnostics, expected.SelectMany(d => d.Filters).ToArray()));
			diagnostics.Clear();
			return results;
		}

		protected static Diagnostic[] FilterDiagnostics(IEnumerable<Diagnostic> diagnostics, params string[] overrides)
		{
			return diagnostics
				.Where(d => d.Id != "CS1701") // Assuming assembly reference 'mscorlib, Version=2.0.0.0' used by 'UnityEngine' matches identity 'mscorlib, Version=4.0.0.0' of 'mscorlib', you may need to supply runtime policy
				.Where(d => d.Id != "CS0414") // IDE0051
				.Where(d => !overrides.Contains(d.Id))
				.ToArray();
		}

		private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
		{
			return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
		}

		private Document[] GetDocuments(string[] sources)
		{
			var project = CreateProject(sources);
			var documents = project.Documents.ToArray();

			if (sources.Length != documents.Length)
			{
				throw new Exception("Amount of sources did not match amount of Documents created");
			}

			return documents;
		}

		protected Document CreateDocument(string source)
		{
			return CreateProject(new[] { source }).Documents.First();
		}

		private static IEnumerable<string> UnityAssemblies()
		{
			var firstInstallationPath = UnityPath.FirstInstallation();
			string installationFullPath = firstInstallationPath;

			if (UnityPath.OnWindows())
			{
				installationFullPath = Path.Combine(firstInstallationPath, "Editor", "Data");

				// Unity installation might be within the Hub directory for Unity Hub installations
				if (!Directory.Exists(installationFullPath))
				{
					string installationHubDirectory = Path.Combine(firstInstallationPath, "Hub", "Editor");
					if (Directory.Exists(installationHubDirectory))
					{
						var editorVersion = Directory.GetDirectories(installationHubDirectory).FirstOrDefault();
						if (editorVersion != null)
						{
							installationFullPath = Path.Combine(installationHubDirectory, editorVersion, "Editor", "Data");
						}
					}
				}
			}

			if (!Directory.Exists(installationFullPath))
				yield break;

			var managed = Path.Combine(installationFullPath, "Managed");
			yield return Path.Combine(managed, "UnityEditor.dll");
			yield return Path.Combine(managed, "UnityEngine.dll");

			var monolib = Path.Combine(installationFullPath, "MonoBleedingEdge", "lib", "mono", "4.7.1-api");
			yield return Path.Combine(monolib, "mscorlib.dll");
			yield return Path.Combine(monolib, "system.dll");
		}

		private Project CreateProject(string[] sources)
		{
			var projectId = ProjectId.CreateNewId(TestProjectName);

			var solution = new AdhocWorkspace()
				.CurrentSolution
				.AddProject(projectId, TestProjectName, TestProjectName, LanguageNames.CSharp);

			solution = UnityAssemblies().Aggregate(solution, (current, dll) => current.AddMetadataReference(projectId, MetadataReference.CreateFromFile(dll)));
			solution = solution.WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion));

			var count = 0;
			foreach (var source in sources)
			{
				var newFileName = DefaultFilePathPrefix + count + "." + CSharpDefaultFileExt;
				var documentId = DocumentId.CreateNewId(projectId, newFileName);
				solution = solution.AddDocument(documentId, newFileName, SourceText.From(source), filePath: @"/" + newFileName);
				count++;
			}

			return solution.GetProject(projectId);
		}

	}
}
