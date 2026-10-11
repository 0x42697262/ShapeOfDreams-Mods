using System;

namespace SodLib.Identity;

internal static partial class ModIdentity
{
	public const string LogPrefix = "[" + Tag + "] ";

	public const string OwnedSuffix = " (" + Tag + ")";

	public const string LobbyButtonName = DisplayName + " Settings Button" + OwnedSuffix;

	public const string ConfigWindowName = DisplayName + " Config" + OwnedSuffix;

	public const string ChangeNoteFile = "changenote.txt";

	public const string ConfigVersionField = "schemaVersion";

	public static bool IsOurs(string objectName)
	{
		return objectName != null && objectName.EndsWith(OwnedSuffix, StringComparison.Ordinal);
	}
}
