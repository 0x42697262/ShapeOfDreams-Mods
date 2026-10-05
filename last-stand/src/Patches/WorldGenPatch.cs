using HarmonyLib;
using LastStand.Arena;
using LastStand.Game;
using LastStand.Mode;

namespace LastStand.Patches;

[HarmonyPatch(typeof(ZoneManager), GameNames.GenerateWorld)]
internal static class WorldGenPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		if (LastStandMode.IsHosting())
		{
			ArenaRoomOverride.Apply(LastStandMod.Current?.Session?.Settings.ArenaRoom);
		}
	}
}
