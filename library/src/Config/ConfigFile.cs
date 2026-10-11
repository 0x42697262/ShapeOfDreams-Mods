using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace SodLib.Config;

internal static class ConfigFile
{
	private const string BackupSuffix = ".old";

	public static void ResetIfOutdated(string path, int currentVersion)
	{
		if (!File.Exists(path) || IsCurrent(path, currentVersion))
		{
			return;
		}

		string backup = path + BackupSuffix;
		File.Copy(path, backup, true);
		File.Delete(path);
		Log.Warn(SharedLogText.ConfigReset(currentVersion, backup));
	}

	private static bool IsCurrent(string path, int currentVersion)
	{
		try
		{
			JToken version = JObject.Parse(File.ReadAllText(path))[ModIdentity.ConfigVersionField];
			return version != null && version.Type == JTokenType.Integer && version.Value<int>() == currentVersion;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
