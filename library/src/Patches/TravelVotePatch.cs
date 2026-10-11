using HarmonyLib;

namespace SodLib.Patches;

[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.ShouldVoteOnTravel))]
internal static class TravelVotePatch
{
	[HarmonyPrefix]
	private static bool Prefix(ref bool __result)
	{
		if (!ModMode.IsHosting() || ModEntry.Active == null || !ModEntry.Active.SkipsTravelVote())
		{
			return true;
		}

		__result = false;
		return false;
	}
}
