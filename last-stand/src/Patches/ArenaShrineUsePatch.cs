using HarmonyLib;
using LastStand.Mode;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Shrine), nameof(Shrine.DoPostUseRoutines))]
internal static class ArenaShrineUsePatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine __instance, Entity entity)
	{
		if (LastStandMode.IsHosting())
		{
			LastStandMod.Current?.Session?.OnShrineUsed(__instance, entity);
		}
	}
}
