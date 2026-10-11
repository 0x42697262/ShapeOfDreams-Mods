using LastStand.Identity;
using Mirror;

namespace LastStand.Mode;

internal static class LastStandMode
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
