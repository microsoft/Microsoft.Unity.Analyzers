using Mono.Addins;
using Mono.Addins.Description;

[assembly: Addin(
	"Microsoft.Unity.Analyzers",
	Category = "Game Development"
)]

[assembly: AddinName("Microsoft.Unity.Analyzers")]
[assembly: AddinCategory("IDE extensions")]
[assembly: AddinDescription("Roslyn Analyzers for Unity developers")]
[assembly: AddinAuthor("Microsoft Corporation")]

[assembly: AddinDependency("MonoDevelop.Core", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency("MonoDevelop.Ide", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency("MonoDevelop.CSharpBinding", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency("MonoDevelop.Refactoring", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency("MonoDevelop.TextEditor", MonoDevelop.BuildInfo.Version)]
