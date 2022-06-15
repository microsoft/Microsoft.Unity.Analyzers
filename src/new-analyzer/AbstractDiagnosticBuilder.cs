/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.IO;

namespace NewAnalyzer;

internal class Diagnostic
{
	public string Id, Name;

	public Diagnostic(string id, string name)
	{
		Id = id;
		Name = name;
	}
}

internal abstract class AbstractDiagnosticBuilder
{
	protected abstract string IdPrefix { get; }

	protected abstract string GetResourceTemplate();
	protected abstract string GetCodeTemplate();
	protected abstract string GetTestTemplate();

	public void Build(string name)
	{
		Build(new Diagnostic(GetNextId(), name));
	}

	public void Build(Diagnostic diagnostic)
	{
		CreateCodeFile(diagnostic);
		CreateTestFile(diagnostic);
		AddResourceEntries(diagnostic);
	}

	private void AddResourceEntries(Diagnostic diagnostic)
	{
		var resx = GetStringsResxPath();
		var entries = Templatize(diagnostic, GetResourceTemplate());

		var file = File.ReadAllText(resx);
		file = file.Replace("</root>", $"{entries}");
		File.WriteAllText(resx, file);
	}

	private static string Templatize(Diagnostic diagnostic, string template)
	{
		return template
			.Replace("$(DiagnosticName)", diagnostic.Name)
			.Replace("$(DiagnosticId)", diagnostic.Id);
	}

	private void CreateCodeFile(Diagnostic diagnostic)
	{
		var file = Path.Combine(GetProjectPath(), diagnostic.Name + ".cs");
		var content = Templatize(diagnostic, GetCodeTemplate());
		File.WriteAllText(file, content);
	}

	private void CreateTestFile(Diagnostic diagnostic)
	{
		var file = Path.Combine(GetTestsProjectPath(), diagnostic.Name + "Tests.cs");
		var content = Templatize(diagnostic, GetTestTemplate());
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
		return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Program).Module.FullyQualifiedName)!, "..", "..", "..", ".."));
	}

	private static string[] GetAllSourceFiles()
	{
		return Directory.GetFiles(GetProjectPath(), "*.cs", SearchOption.AllDirectories);
	}

	private string GetNextId()
	{
		int identifier = 0;

		foreach (var file in GetAllSourceFiles())
		{
			if (TryGetAnalyzerIdentifier(file, out int id))
				identifier = Math.Max(identifier, id);
		}

		identifier++;

		return $"{IdPrefix}{identifier:D4}";
	}

	private bool TryGetAnalyzerIdentifier(string file, out int identifier)
	{
		identifier = -1;

		using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var reader = new StreamReader(fs);

		while (reader.ReadLine() is { } line)
		{
			if (TryExtractIdentifier(line, out int id))
			{
				identifier = Math.Max(identifier, id);
			}
		}

		return identifier != -1;
	}

	private bool TryExtractIdentifier(string? line, out int identifier)
	{
		var declarationStart = $"\"{IdPrefix}";
		identifier = -1;

		if (line == null)
			return false;

		var index = line.LastIndexOf(declarationStart, StringComparison.Ordinal);
		if (index < 0)
			return false;

		index++; // "

		var end = line.IndexOf("\"", ++index, StringComparison.Ordinal);

		index += 3;

		var id = line[index..end];
		return int.TryParse(id, out identifier);
	}
}
