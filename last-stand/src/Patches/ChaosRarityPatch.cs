using HarmonyLib;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Shrine_Chaos), nameof(Shrine_Chaos.SetRandomRarity))]
internal static class ChaosRarityPatch
{
	[HarmonyPostfix]
	private static void Postfix(Shrine_Chaos __instance)
	{
		if (ModMode.IsHosting())
		{
			LastStandMod.Current?.Session?.RerollChaos(__instance);
		}
	}
}
