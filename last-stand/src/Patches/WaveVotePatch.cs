using HarmonyLib;
using LastStand.Game;

namespace LastStand.Patches;

[HarmonyPatch(typeof(ZoneManager), GameNames.VoteStatusUpdate)]
internal static class WaveVotePatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		return !ModMode.IsHosting() || LastStandMod.Current?.Session?.OwnsVote() != true;
	}
}
