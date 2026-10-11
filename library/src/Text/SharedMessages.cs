using System.Collections.Generic;

namespace SodLib.Text;

internal static class SharedMessages
{
	public const string WarningColor = "#ff4d4d";

	public const string SuccessColor = "#5ad45a";

	public const string AnnouncementColor = "#8fd3ff";

	public const string SettingOn = "on";

	public const string SettingOff = "off";

	public const string SettingNumberFormat = "0.###";

	public static string Notice(string text)
	{
		return Colored(ModText.NoticeColor, text);
	}

	public static string Success(string text)
	{
		return Colored(SuccessColor, text);
	}

	public static string Warning(string text)
	{
		return Colored(WarningColor, text);
	}

	public static string Announcement(string text)
	{
		return Colored(AnnouncementColor, text);
	}

	public static string SettingChangeLine(string name, string before, string after)
	{
		return $"{name}: {before} -> {after}";
	}

	public static string SettingListChangeLine(string name, int before, int after, List<string> added,
		List<string> removed)
	{
		string line = $"{name}: {before} -> {after} picked";
		if (added.Count > 0)
		{
			line += $", added {string.Join(", ", added)}";
		}

		if (removed.Count > 0)
		{
			line += $", removed {string.Join(", ", removed)}";
		}

		return line;
	}

	public static string SettingChanged(string line)
	{
		return Notice($"Host changed {line}");
	}

	public static string MoreSettingsChanged(int count)
	{
		return Notice($"...and {count} more setting(s) changed.");
	}

	private static string Colored(string color, string text)
	{
		return $"<color={color}>[{ModIdentity.DisplayName}] {text}</color>";
	}
}
