# Copilot Instructions for Microsoft.Unity.Analyzers

## Project Overview

This is a Roslyn-based analyzer project providing Unity-specific diagnostics and suppressors for Visual Studio. The analyzers help Unity developers by:

1. **Adding Unity-specific diagnostics** (UNTxxxx) - detect Unity-specific issues like performance problems, incorrect API usage, and type safety violations
2. **Suppressing false-positive C# diagnostics** (USPxxxx) - suppress general C# warnings that don't apply to Unity projects (e.g., unused Unity messages, serialized fields)

## Technology Stack

- **Language**: C# with nullable reference types enabled
- **Target Framework**: `netstandard2.0` for analyzers, `net10.0` for tests
- **Roslyn Version**: Microsoft.CodeAnalysis 3.11.0 (locked for compatibility for analyzers, but latest version is used in tests)
- **Testing Framework**: xUnit
- **Build System**: .NET SDK with `slnx` solution files
- **Package Management**: Central Package Management (`Directory.Packages.props`)

## Project Structure

```
src/
├── Microsoft.Unity.Analyzers/       # Main analyzer library
│   ├── Resources/                   # Localized strings (Strings.resx)
│   └── *.cs                         # Analyzer, CodeFix, and Suppressor implementations
├── Microsoft.Unity.Analyzers.Tests/ # Unit tests
│   └── Infrastructure/              # Test helpers and verifiers
├── Microsoft.Unity.Analyzers.Vsix/  # VS extension packaging
└── new-analyzer/                    # Tool to scaffold new analyzers
doc/                                 # Documentation for each diagnostic
```

## Key Conventions

### Code Style

- All rules are located in `src\.editorconfig`
- Use current OS newlines, except for specific rules in `.gitattributes`
- You can use `dotnet format src/Microsoft.Unity.Analyzers.slnx whitespace --verbosity quiet --include <FILE>` to format a specific `<FILE>` (compatible with wildcards)

### File Headers

Every source file must include the Microsoft copyright header:
```csharp
/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/
```

### Creating New Analyzers

Use the scaffolding tool instead of manually creating files:
```bash
# Create analyzer + codefix:
dotnet run --project .\src\new-analyzer MyNewAnalyzer

# Create suppressor:
dotnet run --project .\src\new-analyzer MyNewSuppressor
```

This automatically:
- Creates source files with proper structure
- Adds resource entries to Strings.resx
- Creates corresponding test files
- Use `Run Custom Tool` on `Microsoft.Unity.Analyzers\Resources\String.resx` to re-generate `Microsoft.Unity.Analyzers\src\Microsoft.Unity.Analyzers\Resources\Strings.Designer.cs` using `ResXFileCodeGenerator`

### Diagnostic IDs

- **UNT0001-UNT9999**: Analyzers (Unity diagnostics)
- **USP0001-USP9999**: Suppressors (suppress other analyzer diagnostics)

### Diagnostic Categories

Use constants from `DiagnosticCategory`:
- `Performance` - Performance-related issues
- `Correctness` - Bugs and incorrect behavior
- `TypeSafety` - Type-related issues
- `Readability` - Code clarity issues

## Key Helper Classes

### ScriptInfo

Determines if a type derives from Unity base classes and identifies Unity messages:
```csharp
var scriptInfo = new ScriptInfo(typeSymbol);
if (scriptInfo.HasMessages && scriptInfo.IsMessage(methodSymbol))
{
    // This is a Unity message like Update(), Start(), etc.
}
```

### UnityStubs

Contains stub definitions of Unity types. Located in `UnityStubs.cs`. This is only used for compiling, when we need to know specific method signatures.
When testing, Unity is required and the test infrastructure will use the real `UnityEngine.dll` with effective types.
When you need to search the unity API, you can use :
- https://docs.unity3d.com/ScriptReference/30_search.html?q=QUERY where `QUERY` is your search item
- or if you know the Class/Method name you can use https://docs.unity3d.com/ScriptReference/CLASS.METHOD.html like https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html

### Extension Methods

- `TypeSymbolExtensions` - Type matching and inheritance checks (`Extends()`, `Matches()`)
- `MethodSymbolExtensions` - Method signature matching
- `SyntaxNodeExtensions` - Syntax tree navigation helpers
- `SuppressionAnalysisContextExtensions` - Helper for getting suppressible nodes

## Testing Patterns

### Test Base Classes

- `BaseDiagnosticVerifierTest<TAnalyzer>` - For analyzers without code fixes
- `BaseCodeFixVerifierTest<TAnalyzer, TCodeFix>` - For analyzers with code fixes
- `BaseSuppressorVerifierTest<TAnalyzer>` - For suppressors

### Test Requirements

- **Unity installation required**: Tests use actual Unity assemblies for reference resolution
- Tests automatically inherit `DoNotFailWithInterfaceMembers` test to verify interface member handling
- If you hit `CS0649`, make sure you initialize fields with `null`
- For advanced tests scenarios, use `AnalyzerVerificationContext` to set a specific analyzer option or to filter a diagnostic
- The line numbers in the tests are off by 1, given we are using a verbatim string starting with `@"`, the first line is empty, so the actual code starts on line 2.
- When using DiagnosticMessageFormat parameters, you need to use the method `.WithArguments(...)` on `ExpectDiagnostic`

## Build and Test Commands

```bash
# Build
dotnet build .\src\Microsoft.Unity.Analyzers.slnx

# Run tests
dotnet test .\src\Microsoft.Unity.Analyzers.slnx --filter FullyQualifiedName!~ConsistencyTests
```

Indeed we want to skip ConsistencyTests when developping new diagnostics because those tests are doing http-requests on documentation.

## Documentation

Each diagnostic must have a corresponding documentation file in `doc/`:
- `doc/UNT00XX.md` - Description, examples, and fix guidance
- `doc/index.md` - Master list of all diagnostics

## New Diagnostic Guidelines

- Use the scaffolding tool instead of manually creating files
- Include unit tests for all new functionality
- Update documentation in `doc/` folder
- Ensure build and all tests pass
- When adding tests, try to find edge cases as well
- Follow the existing code style conventions
- Mimic the style, architecture, organization that you can see in existing analyzers and suppressors for consistency
