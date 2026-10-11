using System.Collections.Generic;

namespace SodLib.Mode;

internal static class ModeSelection
{
	public static DewNetworkStartSettings CreateSoloSettings()
	{
		DewNetworkStartSettings settings = new DewNetworkStartSettings();
		Select(settings);
		return settings;
	}

	public static void Select(DewNetworkStartSettings settings)
	{
		settings.addedGameMods = new List<string>();
		settings.customGameSettingsSaveKey = ModIdentity.SaveKey;
	}

	public static void Deselect(DewNetworkStartSettings settings)
	{
		if (settings.customGameSettingsSaveKey == ModIdentity.SaveKey)
		{
			settings.customGameSettingsSaveKey = null;
		}
	}

	public static void StartSolo()
	{
		TransitionManager transitions = ManagerBase<TransitionManager>.softInstance;
		if (transitions != null)
		{
			transitions.PlayGame(CreateSoloSettings());
		}
	}
}
