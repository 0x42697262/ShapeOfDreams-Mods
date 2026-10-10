using System.Collections.Generic;
using DreamArena.Identity;

namespace DreamArena.Mode;

internal static class ModeSelection
{
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
}
