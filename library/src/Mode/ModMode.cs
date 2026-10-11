using Mirror;

namespace SodLib.Mode;

internal static class ModMode
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
