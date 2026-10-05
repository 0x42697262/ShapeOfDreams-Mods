using System;
using HarmonyLib;
using LastStand.Game;
using LastStand.Mode;

namespace LastStand.Patches;

[HarmonyPatch(typeof(BossMonster), GameNames.EntityOnDeath)]
internal static class HeroicBossDeathPatch
{
	[HarmonyPrefix]
	private static bool Prefix(BossMonster __instance, EventInfoKill info)
	{
		if (!LastStandMode.IsHosting() || LastStandMod.Current?.Session?.OwnsHeroicBoss(__instance) != true)
		{
			return true;
		}

		EntityBaseCalls.OnDeath(__instance, info);
		LastStandMod.Current.Session.OnHeroicBossDied(__instance);
		return false;
	}
}

[HarmonyPatch]
internal static class EntityBaseCalls
{
	[HarmonyReversePatch]
	[HarmonyPatch(typeof(Entity), GameNames.EntityOnDeath)]
	public static void OnDeath(Entity instance, EventInfoKill info)
	{
		throw new NotImplementedException();
	}
}
