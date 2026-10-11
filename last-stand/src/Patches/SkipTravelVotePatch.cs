using HarmonyLib;

namespace LastStand.Patches;

[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.ShouldVoteOnTravel))]
internal static class SkipTravelVotePatch
{
	[HarmonyPrefix]
	private static bool Prefix(ref bool __result)
	{
		if (!ModMode.IsHosting())
		{
			return true;
		}

		__result = false;
		return false;
	}
}
