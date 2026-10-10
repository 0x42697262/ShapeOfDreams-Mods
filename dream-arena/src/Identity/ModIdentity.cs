using System;

namespace DreamArena.Identity;

internal static class ModIdentity
{
	public const string Tag = "DreamArena";

	public const string SaveKey = "dreamarena_pvp";

	public const string ChangeNoteFile = "changenote.txt";

	public const string LobbyModeValue = "DreamArena_PvP";

	public const string LogPrefix = "[" + Tag + "] ";

	public const string OwnedSuffix = " (" + Tag + ")";

	public const string LobbyButtonName = "Dream Arena Settings Button" + OwnedSuffix;

	public const string ConfigWindowName = "Dream Arena Config" + OwnedSuffix;

	public const string DummyName = "Training Dummy" + OwnedSuffix;

	public const string DummyLifeEffectId = Tag + "_DummyLife";

	public static string RemnantName(string description)
	{
		return description + OwnedSuffix;
	}

	public static bool IsOurs(string objectName)
	{
		return objectName != null && objectName.EndsWith(OwnedSuffix, StringComparison.Ordinal);
	}
}
