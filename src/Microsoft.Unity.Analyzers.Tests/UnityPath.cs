using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Microsoft.Unity.Analyzers.Tests
{
	internal static class UnityPath
	{
		private static readonly List<string> UnityInstallations = new List<string>();

		public static string FirstInstallation()
		{
			return UnityInstallations.First();
		}

		static UnityPath()
		{
			if (OnWindows())
			{
				RegisterRegistryInstallations();
			}
			else
			{
				RegisterApplicationsInstallations();
			}
		}

		private static void RegisterApplicationsInstallations()
		{
			var directories = Directory.EnumerateDirectories("/Applications/Unity/Hub/Editor")
				.OrderByDescending(n => n)
				.Select(n => Path.Combine(n, "Unity.app"))
				.Concat(new[] {"/Applications/Unity.app"});

			foreach (var name in directories)
			{
				RegisterUnityInstallation(Path.Combine(name, "Contents"));
			}
		}

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
					var unitypath = (string)subkey.GetValue("Location x64");
					if (!string.IsNullOrEmpty(unitypath))
						RegisterUnityInstallation(unitypath);
				}
			}
			catch
			{
			}
		}

		private static void RegisterUnityInstallation(string path)
		{
			if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
				UnityInstallations.Add(path);
		}

		private static bool OnWindows()
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
				case PlatformID.Win32Windows:
					return true;
			}

			return false;
		}
	}
}
