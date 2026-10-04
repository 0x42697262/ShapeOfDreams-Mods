using HarmonyLib;
using Mirror;
using UnityEngine;

namespace DreamArena.Patches;

/// <summary>
/// Loads the arena in its after-the-fight state: just the platform, lit, with
/// no boss-room staging.
/// </summary>
/// <remarks>
/// <para>
/// A boss room loads in its pre-fight state and relies on the intro cutscene
/// to bring the lights up. <c>Room_DarkCave_Boss_0</c> is the clearest case:
/// <c>DarkCaveCutSceneFogFadeOut</c> sets its boss fog to full black on load,
/// and <c>BossZoneLights</c> starts switched off. Only the cutscene's timeline
/// signals clear them. This mod suppresses that cutscene, so the room stayed
/// pitch black.
/// </para>
/// <para>
/// The game already has a state for "this boss room is done":
/// <c>Room.isRevisit</c>, a SyncVar. Rooms read it on every client to present
/// themselves as already cleared. Dark Cave fades its fog out instantly, and
/// the server fires <c>BossSeekerKillBehavior</c>, which brings the sunlight
/// in. Barriers open, and props, monsters and rewards are skipped. Because
/// it is synced, clients without the mod see all of this too. Anything
/// purely visual that the host changed locally would not replicate.
/// </para>
/// <para>
/// Timing: <c>ZoneManager</c> sets <c>isRevisit</c> in the scene-load callback,
/// just before <c>NetworkServer.SpawnObjects</c>, which is what runs
/// <c>Room.OnStartServer</c>. Clients only receive the room once they report
/// ready afterwards, so overriding it here lands in the room's initial state
/// for everyone.
/// </para>
/// <para>
/// Two side effects are handled:
/// </para>
/// <list type="bullet">
/// <item><description>The entry trigger (<c>DewAllHeroesPresentZone</c>)
/// is switched off. It drives the "waiting for all players" message and the
/// intro cutscene plus teleport. It runs only on the server, and the mod
/// already stages players into the arena itself.</description></item>
/// <item><description>A revisit counts as already cleared, so the room's
/// own clear never runs and the exit rift stays shut. That is left as is:
/// opening it put a rift in the middle of the arena floor.</description></item>
/// <item><description>Floor hazards are left on on purpose: Lava Land's
/// lava and Sky's damaging fog make a little environmental challenge.
/// Hazards a boss spawns (such as Ink's damaging ground) never appear, since
/// there is no boss.</description></item>
/// </list>
/// </remarks>
internal static class PlainArena
{
	/// <summary>The room this was applied to, so StartRoom only follows up on it.</summary>
	private static Room _prepared;

	public static bool ShouldApply()
	{
		if (!NetworkServer.active)
		{
			return false;
		}

		DreamArenaMod mod = DreamArenaMod.Instance;
		if (mod == null || !mod.config.arenaEnabled || !mod.config.plainArena)
		{
			return false;
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			return false;
		}

		int index = zones.currentNodeIndex;
		if (index < 0 || index >= zones.nodes.Count)
		{
			return false;
		}

		// The camp stays a normal starting room.
		return zones.nodes[index].type != WorldNodeType.Start;
	}

	public static void Prepare(Room room)
	{
		if (room == null || !ShouldApply())
		{
			_prepared = null;
			return;
		}

		room.isRevisit = true;
		_prepared = room;

		int zones = 0;
		foreach (DewAllHeroesPresentZone zone in
		         Object.FindObjectsByType<DewAllHeroesPresentZone>(
			         FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			zone.enabled = false;
			zones++;
		}

		Debug.Log($"{ModIdentity.LogPrefix}plain arena: '{room.name}' loads in its cleared " +
		          $"state; {zones} entry trigger(s) disabled.");
	}

	public static void AfterStart(Room room)
	{
		if (room == null || room != _prepared)
		{
			return;
		}

		// Deliberately leaves the exit rift alone. A cleared room never opens it
		// on its own, which suits an arena: there is nothing to leave to.
		room.RemoveObstacles();
	}
}

[HarmonyPatch(typeof(Room), nameof(Room.OnStartServer))]
internal static class PlainArenaStartServerPatch
{
	[HarmonyPostfix]
	private static void Postfix(Room __instance)
	{
		PlainArena.Prepare(__instance);
	}
}

[HarmonyPatch(typeof(Room), nameof(Room.StartRoom))]
internal static class PlainArenaStartRoomPatch
{
	[HarmonyPostfix]
	private static void Postfix(Room __instance)
	{
		PlainArena.AfterStart(__instance);
	}
}
