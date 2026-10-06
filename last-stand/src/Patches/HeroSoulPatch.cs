using System;
using HarmonyLib;
using LastStand.Combat;
using LastStand.Diagnostics;
using LastStand.Mode;
using LastStand.Text;

namespace LastStand.Patches;

[HarmonyPatch(typeof(Se_HeroKnockedOut), nameof(Se_HeroKnockedOut.CheckAndAddHeroSoul))]
internal static class HeroSoulPatch
{
	[HarmonyPrefix]
	private static bool Prefix(Se_HeroKnockedOut __instance)
	{
		if (!LastStandMode.IsHosting())
		{
			return true;
		}

		try
		{
			return !HeroSouls.PlaceInCurrentRoom(__instance);
		}
		catch (Exception e)
		{
			Log.Error(LogText.HeroSoulFailed, e);
			return true;
		}
	}
}
