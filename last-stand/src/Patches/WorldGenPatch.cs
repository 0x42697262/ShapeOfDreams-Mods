using HarmonyLib;
using LastStand.Game;

namespace LastStand.Patches;

[HarmonyPatch(typeof(ZoneManager), SharedGameNames.GenerateWorld)]
internal static class WorldGenPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		if (ModMode.IsHosting())
		{
			LastStandMod.Current?.Session?.ApplyArenaRoom();
		}
	}
}
