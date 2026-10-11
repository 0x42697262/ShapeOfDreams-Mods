using System;
using HarmonyLib;
using LastStand.Combat;
using LastStand.Text;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Se_HeroKnockedOut), nameof(Se_HeroKnockedOut.CheckAndAddHeroSoul))]
internal static class HeroSoulPatch
{
	[HarmonyPrefix]
	private static bool Prefix(Se_HeroKnockedOut __instance)
	{
		if (!ModMode.IsHosting())
		{
			return true;
		}

		try
		{
			HeroSouls.Suppress(__instance);
		}
		catch (Exception e)
		{
			Log.Error(LogText.HeroSoulFailed, e);
		}

		return false;
	}
}
