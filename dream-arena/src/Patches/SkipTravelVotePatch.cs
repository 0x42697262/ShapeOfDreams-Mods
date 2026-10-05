using HarmonyLib;
using Mirror;

namespace DreamArena.Patches;

/// <summary>
/// Travels straight through a rift instead of holding a world-map vote.
/// </summary>
/// <remarks>
/// <para>
/// With more than one hero alive, <c>ZoneManager.CmdTravelToNode</c> does not
/// travel. It calls <c>StartVoteNextNode</c>, which opens the world map for
/// everyone with a 6-second "travel?" vote. Every node leads to the same
/// arena, so the map has nothing to choose.
/// </para>
/// <para>
/// The command runs on the host, and its only test is
/// <c>ShouldVoteOnTravel()</c>. Returning false there makes it call
/// <c>TravelToNode</c> directly, so vanilla clients travel too. A client still
/// evaluates its own copy in <c>TravelWithValidationAndConfirmation</c>, but
/// there it only decides whether to show the "can't travel yet" reasons, so
/// the mismatch is harmless.
/// </para>
/// <para>
/// This patches the check instead of clearing the synced
/// <c>GameSettingsManager.enableVotes</c> setting. The lobby saves that setting
/// as the host's preference when a game starts, so clearing it would leak into
/// normal runs.
/// </para>
/// </remarks>
[HarmonyPatch(typeof(ZoneManager), nameof(ZoneManager.ShouldVoteOnTravel))]
internal static class SkipTravelVotePatch
{
	[HarmonyPrefix]
	private static bool Prefix(ref bool __result)
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (!NetworkServer.active || mod == null
		    || !mod.config.arenaEnabled || !mod.config.skipTravelVote)
		{
			return true;
		}

		__result = false;
		return false;
	}
}
