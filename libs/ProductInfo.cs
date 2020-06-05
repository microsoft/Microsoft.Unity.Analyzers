﻿using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using Microsoft.Win32;
using SyntaxTree.VisualStudio.Unity;

[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyConfiguration("")]

[assembly: AssemblyProduct(ProductInfo.ProductName)]

[assembly: AssemblyCopyright("")] // autogenerated, see tools/buildinfos.ps1
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: NeutralResourcesLanguage("en-US")]

[assembly: AssemblyVersion(ProductInfo.Version)]
[assembly: AssemblyFileVersion(ProductInfo.Version)]
[assembly: AssemblyInformationalVersion("")] // autogenerated, see tools/buildinfos.ps1

namespace SyntaxTree.VisualStudio.Unity
{
	internal class ProductInfo
	{
		public const string Version = "1.7.0.0";
		public const string ProductName = "Microsoft Unity Analyzers";
	}
}
