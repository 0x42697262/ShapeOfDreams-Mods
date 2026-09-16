using HarmonyLib;

namespace DreamArena.Patches;

/// <summary>
/// Redirects the world to the arena room as soon as it is generated.
/// </summary>
/// <remarks>
/// <para>
/// This is the fix for the override silently doing nothing. Applying it when
/// the game manager starts was too early — the node list was still empty, so
/// the log read "applied to 0 node(s)" and the run played out normally.
/// </para>
/// <para>
/// <c>GenerateWorld_Imp</c> is where <c>nodes</c> is actually filled
/// (<c>nodes.Clear(); nodes.AddRange(newNodes);</c>). A postfix here sees the
/// finished list.
/// </para>
/// <para>
/// It also runs at the safest possible moment: the method ends by setting
/// <c>currentNodeIndex = -1</c>, so nobody is standing in a room yet and the
/// game's own "tried to set room override on current node" warning cannot
/// trigger.
/// </para>
/// </remarks>
[HarmonyPatch(typeof(ZoneManager), "GenerateWorld_Imp")]
internal static class WorldGenPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null)
		{
			return;
		}

		ArenaRules.ApplyArenaRoom(mod.config);
	}
}
