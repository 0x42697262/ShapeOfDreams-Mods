using System;
using System.IO;
using DreamArena.Diagnostics;
using DreamArena.Text;

namespace DreamArena.Config;

internal static class ConfigFile
{
	private const string BackupSuffix = ".old";

	public static void ResetIfOutdated(string path)
	{
		if (!File.Exists(path) || IsCurrent(path))
		{
			return;
		}

		string backup = path + BackupSuffix;
		File.Copy(path, backup, true);
		File.Delete(path);
		Log.Warn(LogText.ConfigReset(ConfigSchema.Current, backup));
	}

	private static bool IsCurrent(string path)
	{
		try
		{
			return ConfigSchema.IsCurrent(File.ReadAllText(path));
		}
		catch (Exception)
		{
			return false;
		}
	}
}
