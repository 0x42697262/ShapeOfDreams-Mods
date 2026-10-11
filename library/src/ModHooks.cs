using System;
using System.Collections.Generic;

namespace SodLib;

internal static partial class ModHooks
{
	public static partial IReadOnlyList<Choice> Choices(Enum set);

	public static partial string LobbyName();

	public static ModBehaviour Behaviour()
	{
		return ModEntry.Active;
	}

	public static ModConfig Config()
	{
		return ModEntry.Active?.Config;
	}

	public static void ApplyConfig(ModConfig edited)
	{
		ModEntry.Active?.ApplyConfig(edited);
	}
}
