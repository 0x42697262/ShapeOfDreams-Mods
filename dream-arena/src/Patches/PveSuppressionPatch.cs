using HarmonyLib;

namespace DreamArena.Patches;

/// <summary>
/// Stops the arena room from spawning its props.
/// </summary>
/// <remarks>
/// Wells, gold and dream-dust stones, merchants and shrines are spawned by
/// <c>RoomProps.OnRoomStartServer</c>, which is independent of the monster
/// population budget — so zeroing that budget never touched them.
///
/// Skipping the whole method is what keeps the arena clean. The camp is
/// unaffected, since suppression only applies once players are through the
/// rift.
/// </remarks>
[HarmonyPatch(typeof(RoomProps), "OnRoomStartServer")]
internal static class RoomPropsPatch
{
	[HarmonyPrefix]
	private static bool Prefix()
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null)
		{
			return true;
		}

		// Run the original unless this room is meant to be a bare arena.
		return !PveSuppressor.ShouldSuppress(mod.config, mod.Phase);
	}
}
