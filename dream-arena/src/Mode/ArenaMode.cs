using DreamArena.Identity;
using Mirror;

namespace DreamArena.Mode;

internal static class ArenaMode
{
	public static bool IsSelected()
	{
		GameSettingsManager settings = NetworkedManagerBase<GameSettingsManager>.softInstance;
		return settings != null && settings.gameSettingsSaveKey == ModIdentity.SaveKey;
	}

	public static bool IsHosting()
	{
		return NetworkServer.active && IsSelected();
	}
}
