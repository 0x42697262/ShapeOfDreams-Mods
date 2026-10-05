using HarmonyLib;
using LastStand.Mode;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Shrine), nameof(Shrine.DoPostUseRoutines))]
internal static class DreamWellUsePatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine __instance)
	{
		if (LastStandMode.IsHosting())
		{
			LastStandMod.Current?.Session?.Well.OnUsed(__instance);
		}
	}
}
