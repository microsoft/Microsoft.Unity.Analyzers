/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Unity.Analyzers.Tests
{
	public class UnityAnalyzerVerifier<TAnalyzer>
		where TAnalyzer : DiagnosticAnalyzer, new()
	{
		public static DiagnosticResult Diagnostic()
			=> CSharpAnalyzerVerifier<TAnalyzer, XUnitVerifier>.Diagnostic();

		public static DiagnosticResult Diagnostic(string diagnosticId)
			=> CSharpAnalyzerVerifier<TAnalyzer, XUnitVerifier>.Diagnostic(diagnosticId);

		public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
			=> new DiagnosticResult(descriptor);

		public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
		{
			var test = new UnityAnalyzerTest { TestCode = source };
			test.ExpectedDiagnostics.AddRange(expected);
			return test.RunAsync();
		}

		protected static IEnumerable<string> UnityAssemblies()
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
					string editorVersion;
					if (Directory.Exists(installationHubDirectory))
					{
						editorVersion = Directory.GetDirectories(installationHubDirectory).FirstOrDefault();
						if (editorVersion != null)
						{
							installationFullPath = Path.Combine(installationHubDirectory, editorVersion, "Editor", "Data");
						}
					}
				}
			}

			if (Directory.Exists(installationFullPath))
			{
				var managed = Path.Combine(installationFullPath, "Managed");
				yield return Path.Combine(managed, "UnityEditor.dll");
				yield return Path.Combine(managed, "UnityEngine.dll");
			}
		}

		public class UnityAnalyzerTest : CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>
		{
			public UnityAnalyzerTest()
			{
				this.SolutionTransforms.Add((s, pid) =>
				{
					foreach (var asm in UnityAssemblies())
					{
						s = s.AddMetadataReference(pid, MetadataReference.CreateFromFile(asm));
					}

					return s;
				});
			}
		}
	}

	public class UnityCodeFixVerifier<TAnalyzer, TCodeFix> : UnityAnalyzerVerifier<TAnalyzer>
		where TAnalyzer : DiagnosticAnalyzer, new()
		where TCodeFix : CodeFixProvider, new()
	{
		public static Task VerifyCodeFixAsync(string source, string fixedSource)
			=> VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

		public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
			=> VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

		public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
		{
			var test = new UnityCodeFixTest { TestCode = source, FixedCode = fixedSource, };

			test.ExpectedDiagnostics.AddRange(expected);
			return test.RunAsync();
		}

		public class UnityCodeFixTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
		{
			public UnityCodeFixTest()
			{
				this.SolutionTransforms.Add((s, pid) =>
				{
					foreach (var asm in UnityAssemblies())
					{
						s = s.AddMetadataReference(pid, MetadataReference.CreateFromFile(asm));
					}

					return s;
				});
			}
		}
	}
}
