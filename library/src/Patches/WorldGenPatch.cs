using HarmonyLib;

namespace SodLib.Patches;

[HarmonyPatch(typeof(ZoneManager), SharedGameNames.GenerateWorld)]
internal static class WorldGenPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		if (ModMode.IsHosting())
		{
			ModEntry.Active?.WorldGenerated();
		}
	}
}
