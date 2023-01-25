/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.Unity.Analyzers.Tests;

internal static class UnityPath
{
	private static readonly List<string> UnityInstallations = new();

	public static string FirstInstallation()
	{
		return UnityInstallations.First();
	}

	static UnityPath()
	{
		if (OperatingSystem.IsWindows())
			RegisterWindowsInstallations();

		if (OperatingSystem.IsMacOS())
			RegisterMacOsInstallations();

		if (OperatingSystem.IsLinux())
			RegisterLinuxInstallations();

		if (UnityInstallations.Count == 0)
			throw new Exception("Could not locate a Unity installation");
	}

	private static void RegisterHubInstallations(string hubBasePath, string editorSubFolder = "Editor", string dataSubFolder = "Data")
	{
		var hubPath = Path.Combine(hubBasePath, "Unity", "Hub", "Editor");
		if (!Directory.Exists(hubPath))
			return;

		var directories = Directory.EnumerateDirectories(hubPath)
			.OrderByDescending(n => n)
			.Select(n => Path.Combine(n, editorSubFolder));

		foreach (var name in directories)
			RegisterUnityInstallation(Path.Combine(name, dataSubFolder));
	}

	private static void RegisterMacOsInstallations()
	{
		RegisterHubInstallations(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity.app", "Contents");
		RegisterUnityInstallation(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity/Unity.app/Contents"));
	}

	private static void RegisterLinuxInstallations()
	{
		RegisterHubInstallations(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
		RegisterUnityInstallation(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Unity/Editor/Data"));
	}

	[SupportedOSPlatform("windows")]
	private static void RegisterWindowsInstallations()
	{
		var programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");

		RegisterHubInstallations(programFiles);
		RegisterUnityInstallation(Path.Combine(programFiles, @"Unity\Editor\Data"));
		RegisterRegistryInstallations();
	}

	[SupportedOSPlatform("windows")]
	private static void RegisterRegistryInstallations()
	{
		var hive = Registry.CurrentUser;

		try
		{
			var installerkey = hive.OpenSubKey(@"Software\Unity Technologies\Installer\", writable: false);
			if (installerkey == null)
				return;

			var names = installerkey.GetSubKeyNames().Where(n => n.StartsWith("Unity")).OrderByDescending(n => n);

			foreach (var name in names)
			{
				var subkey = installerkey.OpenSubKey(name);
				if (subkey == null)
					continue;

				// x64 is the default Unity installation
				var unitypath = subkey.GetValue("Location x64") as string;
				if (!string.IsNullOrEmpty(unitypath))
					RegisterUnityInstallation(Path.Combine(unitypath, @"Editor\Data"));
			}
		}
		catch (Exception e)
		{
			Assert.True(false, e.ToString());
		}
	}

	private static void RegisterUnityInstallation(string path)
	{
		if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
			UnityInstallations.Add(path);
	}
}
