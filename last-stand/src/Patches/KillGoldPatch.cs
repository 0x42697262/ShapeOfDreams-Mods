using HarmonyLib;
using LastStand.Mode;
using LastStand.Session;

namespace LastStand.Patches;

[HarmonyPatch(typeof(GameManager), nameof(GameManager.GetKillGoldAmount))]
internal static class KillGoldPatch
{
	[HarmonyPostfix]
	private static void Postfix(ref int __result)
	{
		if (__result <= 0 || !LastStandMode.IsHosting())
		{
			return;
		}

		LastStandSession session = LastStandMod.Current?.Session;
		if (session != null)
		{
			__result = session.CapKillGold(__result);
		}
	}
}
