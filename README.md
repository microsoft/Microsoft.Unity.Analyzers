# Analyzers for Unity

[![Build status on Windows](https://github.com/microsoft/Microsoft.Unity.Analyzers/workflows/CI-Windows/badge.svg)](https://github.com/microsoft/Microsoft.Unity.Analyzers/actions?query=workflow%3ACI-Windows)
[![Build status on macOS](https://github.com/microsoft/Microsoft.Unity.Analyzers/workflows/CI-macOS/badge.svg)](https://github.com/microsoft/Microsoft.Unity.Analyzers/actions?query=workflow%3ACI-macOS)

This project provides Visual Studio with a better understanding of Unity projects by adding Unity-specific diagnostics or by removing general C# diagnostics that do not apply to Unity projects. 

Check out the [list of analyzers and suppressors](doc/index.md) defined in this project.

If you have an idea for a best practice for Unity developers to follow, please open an [issue](https://github.com/microsoft/Microsoft.Unity.Analyzers/issues/new?template=Feature_request.md) with the description.

# Prerequisites
For building and testing, you'll need the **.NET Core SDK Version 3.1.100 (LTS)**.

This project is targeting **Visual Studio 2019 16.4** and **Visual Studio for Mac 8.4**.

This project is using the `DiagnosticSuppressor` API to conditionally suppress reported compiler/analyzer diagnostics. 

On Windows, you'll need the _Visual Studio extension development_ workload installed to build a VSIX to use and debug the project in Visual Studio.

For unit-testing, we require Unity to be installed. We recommend using the latest LTS version for that.

# Building and testing

Compiling the solution:
`dotnet build .\src\Microsoft.Unity.Analyzers.sln`

Running the unit tests:
`dotnet test .\src\Microsoft.Unity.Analyzers.sln`

You can open `.\src\Microsoft.Unity.Analyzers.sln` in your favorite IDE to work on the analyzers and run/debug the tests.

# Debugging the analyzers on a Unity project

Running and debugging the tests is the easiest way to get started but sometimes you want to work on a real-life Unity project.

## On Windows

- Open the `Microsoft.Unity.Analyzers.Vsix.sln` solution.
- Make sure `Microsoft.Unity.Analyzers.Vsix` is set as the startup project.
- Hit play (Current Instance) to start debugging an experimental instance of Visual Studio 2019.
- Load any Unity project in the Visual Studio experimental instance then put breakpoints in the `Microsoft.Unity.Analyzers` project.

## On macOS

- Open the `Microsoft.Unity.Analyzers.Mpack.sln` solution.
- Make sure `Microsoft.Unity.Analyzers.Mpack` is set as the startup project.
- Hit play to start debugging an experimental instance of Visual Studio for Mac.
- Load any Unity project in the Visual Studio for Mac experimental instance then put breakpoints in the `Microsoft.Unity.Analyzers` project.

# Handling duplicate diagnostics 

Starting with **Visual Studio Tools for Unity 4.3.2.0 (or 2.3.2.0 on MacOS)**, we ship and automatically include this set of analyzers/suppressors in all projects generated by Unity (using `<Analyzer Include="..." />` directive).

The downside of this is when trying to debug your own solution is to find yourself with duplicated diagnostics because Visual Studio will load both:
- the project-local analyzer that we release and include automatically, through the `<Analyzer Include="..." />` directive. 
- the VSIX extension you deployed, that will apply analyzers/suppressors to all projects in the IDE.

To disable the project-local analyzer, and keeping a workflow compatible with Unity re-generating project files on all asset changes, you can add the following script in an `Editor` folder of your Unity project to disable all local analyzers loaded with `<Analyzer Include="..." />` directive.

```csharp
using UnityEditor;
using System.Text.RegularExpressions;

public class DisableLocalAnalyzersPostProcessor : AssetPostprocessor
{
	public static string OnGeneratedCSProject(string path, string content)
	{
		return Regex.Replace(content, "(\\<Analyzer)\\s+(Include=\".*Microsoft\\.Unity\\.Analyzers\\.dll\")", "$1 Condition=\"false\" $2");
	}
}
```

# Creating a new analyzer 

To easily create a new analyzer, you can use the following command:

`dotnet run --project .\src\new-analyzer`

This will automatically create source files for the analyzer, associated tests and add resource entries. If your new analyzer's name contains the word `suppressor`, the tool will create a new suppressor. By default the tool will create a regular analyzer and codefix.

Example for creating `CustomAnalyzer`, `CustomCodeFix` and `CustomTests` classes :

`dotnet run --project .\src\new-analyzer Custom`

Example for creating `CustomSuppressor` and `CustomSuppressorTests` classes :

`dotnet run --project .\src\new-analyzer CustomSuppressor`

# Contributing

This project welcomes contributions and suggestions.
Please have a look at our [Guidelines](CONTRIBUTING.md) for contributing.
