using HarmonyLib;
using LastStand.Mode;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Shrine), nameof(Shrine.DoPostUseRoutines))]
internal static class ArenaShrineUsePatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine __instance)
	{
		if (LastStandMode.IsHosting())
		{
			LastStandMod.Current?.Session?.Shrines.OnUsed(__instance);
		}
	}
}
