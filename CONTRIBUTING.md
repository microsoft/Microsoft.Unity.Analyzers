# Contributing

This project has adopted the [Microsoft Open Source Code of
Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct
FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com)
with any additional questions or comments.

## Prerequisites

The only prerequisite for building, testing, and deploying from this repository
is the [.NET SDK](https://get.dot.net/).
You should install at least version 3.0 (compatible with C# 8.0).

This repository can be built on Windows, Linux, and OSX.

## Package restore

To restore packages use `dotnet restore`.

## Building

Building, testing, and packing this repository can be done by using the standard dotnet CLI commands (e.g. `dotnet build`, `dotnet test`, `dotnet pack`, etc.).

## Bug reports

If you have a bug report, please file an issue. 
If you can send a pull request with a repro of the bug in the form of a unit test, please do submit that PR
and link to it from the Issue you file.

## Pull Requests

We love to get pull requests. If you have a bug fix to offer or a new analyzer, please send us a pull request.

Every new feature or bug fix should be accompanied by unit tests to cover your change.
