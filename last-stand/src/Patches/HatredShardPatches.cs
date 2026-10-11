using HarmonyLib;
using LastStand.Game;
using LastStand.Mode;
using LastStand.Well;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Shrine_Hatred_Reward), GameNames.ShrineOnUse)]
internal static class HatredShardUsePatch
{
	[HarmonyPrefix]
	private static void Prefix(Shrine_Hatred_Reward __instance)
	{
		if (LastStandMode.IsHosting())
		{
			LastStandMod.Current?.Session?.OnHatredShardOpening(__instance);
		}
	}

	[HarmonyFinalizer]
	private static void Finalizer()
	{
		HatredShards.Close();
	}
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.GetSpecialRewardAmount_Gold))]
internal static class HatredShardGoldPatch
{
	[HarmonyPostfix]
	private static void Postfix(ref float __result)
	{
		if (HatredShards.TryGetGold(out float amount))
		{
			__result = amount;
		}
	}
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.GetSpecialRewardAmount_DreamDust))]
internal static class HatredShardDreamDustPatch
{
	[HarmonyPostfix]
	private static void Postfix(ref float __result)
	{
		if (HatredShards.TryGetDreamDust(out float amount))
		{
			__result = amount;
		}
	}
}
