using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Generic;
using System.IO;
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

        public class UnityAnalyzerTest : CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>
        {
            private static IEnumerable<string> UnityAssemblies()
            {
                var installation = UnityPath.FirstInstallation();
                var managed = Path.Combine(installation, "Editor", "Data", "Managed");
                yield return Path.Combine(managed, "UnityEditor.dll");
                yield return Path.Combine(managed, "UnityEngine.dll");
            }

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
            var test = new UnityCodeFixTest
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            //if (fixedSource == source)
            //{
            //    test.FixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
            //    test.FixedState.MarkupHandling = MarkupMode.Allow;
            //    test.BatchFixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
            //    test.BatchFixedState.MarkupHandling = MarkupMode.Allow;
            //}

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync();
        }

        public class UnityCodeFixTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        {
            private static IEnumerable<string> UnityAssemblies()
            {
                var installation = UnityPath.FirstInstallation();
                var managed = Path.Combine(installation, "Editor", "Data", "Managed");
                yield return Path.Combine(managed, "UnityEditor.dll");
                yield return Path.Combine(managed, "UnityEngine.dll");
            }

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

