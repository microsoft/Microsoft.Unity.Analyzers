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
		{
			RegisterRegistryInstallations();
		}
		else if (OperatingSystem.IsMacOS())
		{
			RegisterApplicationsInstallations();
		}
		else if (OperatingSystem.IsLinux())
		{
			RegisterLinuxInstallations();
		}
		else
		{
			throw new NotImplementedException("operating system not supported");
		}

		if (UnityInstallations.Count == 0) throw new Exception("could not locate a unity installation");
	}

	private static void RegisterApplicationsInstallations()
	{
		const string hub = "/Applications/Unity/Hub/Editor";
		if (Directory.Exists(hub))
		{
			var directories = Directory.EnumerateDirectories(hub)
				.OrderByDescending(n => n)
				.Select(n => Path.Combine(n, "Unity.app"));

			foreach (var name in directories)
			{
				RegisterUnityInstallation(Path.Combine(name, "Contents"));
			}
		}

		RegisterUnityInstallation("/Applications/Unity/Unity.app/Contents");
	}

	private static void RegisterLinuxInstallations()
	{
		string hub = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Unity/Hub/Editor");
		if (Directory.Exists(hub))
		{
			var directories = Directory.EnumerateDirectories(hub)
				.OrderByDescending(n => n)
				.Select(n => Path.Combine(n, "Editor"));

			foreach (var name in directories)
			{
				RegisterUnityInstallation(Path.Combine(name,"Data"));
			}
		}
		else
		{
			throw new DirectoryNotFoundException($"could not locate unity hub at '{hub}'");
		}
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
					RegisterUnityInstallation(unitypath);
			}
		}
		catch (Exception e)
		{
			Assert.True(false, e.ToString());
		}
		finally
		{
			// default fallback, newer Unity are all x64, so we always want the 'real' program files, even in x64 or AnyCPU targets
			var programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
			RegisterUnityInstallation(Path.Combine(programFiles, "Unity"));
		}
	}

	private static void RegisterUnityInstallation(string path)
	{
		if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
			UnityInstallations.Add(path);
	}
}
