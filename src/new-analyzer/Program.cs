/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.IO;

namespace NewAnalyzer
{
	class Diagnostic
	{
		public string Id, Name;
	}

	class Program
	{
		static void Main(string[] args)
		{
			string name;
			if (args.Length == 0)
			{
				Console.Write("Diagnostic name: ");
				name = Console.ReadLine();
			}
			else
			{
				name = args[0];
			}

			var diagnostic = new Diagnostic {Id = GetNextId(), Name = name,};

			CreateCodeFile(diagnostic);
			CreateTestFile(diagnostic);
			AddResourceEntries(diagnostic);
		}

		private static void AddResourceEntries(Diagnostic diagnostic)
		{
			string template =
				@"  <data name=""$(DiagnosticName)CodeFixTitle"" xml:space=""preserve"">
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

			var resx = GetStringsResxPath();
			var entries = Templatize(diagnostic, template);

			var file = File.ReadAllText(resx);
			file = file.Replace("</root>", entries);
			File.WriteAllText(resx, file);
		}

		private static string Templatize(Diagnostic diagnostic, string template)
		{
			return template
				.Replace("$(DiagnosticName)", diagnostic.Name)
				.Replace("$(DiagnosticId)", diagnostic.Id);
		}

		private static void CreateCodeFile(Diagnostic diagnostic)
		{
			string template =
				@"using System.Collections.Immutable;
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
		public const string Id = ""$(DiagnosticId)"";

		private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			Id,
			title: Strings.$(DiagnosticName)DiagnosticTitle,
			messageFormat: Strings.$(DiagnosticName)DiagnosticMessageFormat,
			//category: DiagnosticCategory._,
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
		public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create($(DiagnosticName)Analyzer.Id);

		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			//var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			//var declaration = root.FindNode(context.Span) as MethodDeclarationSyntax;
			//if (declaration == null)
			//    return;

			//context.RegisterCodeFix(
			//    CodeAction.Create(
			//        Strings.$(DiagnosticName)CodeFixTitle,
			//        ct => {},
			//        declaration.ToFullString()),
			//    context.Diagnostics);
		}
	}
}
";

			var file = Path.Combine(GetProjectPath(), diagnostic.Name + ".cs");
			var content = Templatize(diagnostic, template);
			File.WriteAllText(file, content);
		}

		private static void CreateTestFile(Diagnostic diagnostic)
		{
			string template =
				@"using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests
{
	using Verify = UnityCodeFixVerifier<$(DiagnosticName)Analyzer, $(DiagnosticName)CodeFix>;

	public class $(DiagnosticName)Tests
	{
		[Fact]
		public async Task Test ()
		{
			const string test = @""
using UnityEngine;

class Camera : MonoBehaviour
{
}
"";

			var diagnostic = Verify.Diagnostic();

			const string fixedTest = @""
using UnityEngine;

class Camera : MonoBehaviour
{
}
"";
			await Verify.VerifyCodeFixAsync(test, diagnostic, fixedTest);
		}
	}
}
";

			var file = Path.Combine(GetTestsProjectPath(), diagnostic.Name + "Tests.cs");
			var content = Templatize(diagnostic, template);
			File.WriteAllText(file, content);
		}

		private static string GetStringsResxPath()
		{
			return Path.Combine(GetProjectPath(), "Resources", "Strings.resx");
		}

		private static string GetProjectPath()
		{
			return Path.Combine(GetSourcePath(), "Microsoft.Unity.Analyzers");
		}

		private static string GetTestsProjectPath()
		{
			return Path.Combine(GetSourcePath(), "Microsoft.Unity.Analyzers.Tests");
		}

		private static string GetSourcePath()
		{
			return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Program).Module.FullyQualifiedName), "..", "..", "..", ".."));
		}

		private static string[] GetAllSourceFiles()
		{
			return Directory.GetFiles(GetProjectPath(), "*.cs", SearchOption.AllDirectories);
		}

		private static string GetNextId()
		{
			int identifier = 0;

			foreach (var file in GetAllSourceFiles())
			{
				if (TryGetAnalyzerIdentifier(file, out int id))
					identifier = Math.Max(identifier, id);
			}

			identifier++;

			return string.Format("UNT{0:D4}", identifier);
		}

		private static bool TryGetAnalyzerIdentifier(string file, out int identifier)
		{
			identifier = -1;

			using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				using (var reader = new StreamReader(fs))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						if (TryExtractIdentifier(line, out int id))
						{
							identifier = Math.Max(identifier, id);
						}
					}
				}
			}

			return identifier != -1;
		}

		private static bool TryExtractIdentifier(string line, out int identifier)
		{
			const string declarationStart = "\"UNT";
			identifier = -1;

			var index = line.LastIndexOf(declarationStart);
			if (index < 0)
				return false;

			index++; // "

			var end = line.IndexOf("\"", ++index);

			index += 3; // UNT

			var id = line.Substring(index, end - index);

			return int.TryParse(id, out identifier);
		}
	}
}
