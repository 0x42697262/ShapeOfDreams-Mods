using System;

namespace LastStand.Identity;

internal static class ModIdentity
{
	public const string Tag = "LastStand";

	public const string SaveKey = "laststand";

	public const string ChangeNoteFile = "changenote.txt";

	public const string LobbyModeValue = "LastStand_Mode";

	public const string LogPrefix = "[" + Tag + "] ";

	public const string OwnedSuffix = " (" + Tag + ")";

	public const string SoloButtonName = "Last Stand Button" + OwnedSuffix;

	public const string LobbyButtonName = "Last Stand Settings Button" + OwnedSuffix;

	public const string ConfigWindowName = "Last Stand Config" + OwnedSuffix;

	public static bool IsOurs(string objectName)
	{
		return objectName != null && objectName.EndsWith(OwnedSuffix, StringComparison.Ordinal);
	}
}
