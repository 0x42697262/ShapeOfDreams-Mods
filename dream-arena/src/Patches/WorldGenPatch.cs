using HarmonyLib;
using DreamArena.Game;

namespace DreamArena.Patches;

[HarmonyPatch(typeof(ZoneManager), SharedGameNames.GenerateWorld)]
internal static class WorldGenPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		DreamArenaMod.Current?.Session?.OnWorldGenerated();
	}
}
