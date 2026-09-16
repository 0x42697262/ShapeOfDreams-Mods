using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Points the camp's exit rift straight at the arena.
/// </summary>
/// <remarks>
/// <para>
/// Stock behaviour opens the world map and asks which node to travel to, which
/// is pointless when every node is the same arena.
/// </para>
/// <para>
/// <c>Rift_RoomExit</c> already supports a direct route. Its
/// <c>UserCode_TpcInteract</c> checks <c>nextNodeIndex</c> first:
/// </para>
/// <code>
/// if (nextNodeIndex >= 0) { confirm -> CmdTravelToNode(nextNodeIndex); return; }
/// if (currentNode.type != ExitBoss &amp;&amp; !forceTravelToNextZone) { show world map; return; }
/// </code>
/// <para>
/// So setting <c>nextNodeIndex</c> gives exactly the boss-room feel: a single
/// "move to the next location" confirmation, then straight into the fight.
/// It is a SyncVar, so the host setting it is enough.
/// </para>
/// </remarks>
internal static class CampRift
{
	private const float RetryInterval = 1f;

	private static float _timer;
	private static int _lastTarget = -1;

	public static void Tick(float deltaTime, DreamArenaConfig config, PhaseKind phase)
	{
		if (!NetworkServer.active || config == null || !config.arenaEnabled)
		{
			return;
		}

		// Only the camp's rift needs redirecting.
		if (phase != PhaseKind.Camp)
		{
			_lastTarget = -1;
			return;
		}

		_timer += deltaTime;
		if (_timer < RetryInterval)
		{
			return;
		}

		_timer = 0f;
		Apply();
	}

	private static void Apply()
	{
		// softInstance avoids the logged complaint when no rift exists yet.
		Rift_RoomExit rift = Rift_RoomExit.softInstance;
		if (rift.IsNullOrInactive())
		{
			return;
		}

		ZoneManager zones = NetworkedManagerBase<ZoneManager>.instance;
		if (zones == null)
		{
			return;
		}

		if (!TryFindArenaNode(zones, out int target))
		{
			return;
		}

		if (rift.nextNodeIndex == target && _lastTarget == target)
		{
			return;
		}

		rift.NetworknextNodeIndex = target;
		_lastTarget = target;

		Debug.Log($"{ModIdentity.LogPrefix}camp rift now leads straight to node {target}.");
	}

	/// <summary>
	/// Picks the arena node to send players to: a connected non-camp node,
	/// preferring one reachable from where they stand.
	/// </summary>
	private static bool TryFindArenaNode(ZoneManager zones, out int target)
	{
		target = -1;

		int current = zones.currentNodeIndex;
		int fallback = -1;

		for (int i = 0; i < zones.nodes.Count; i++)
		{
			if (i == current || zones.nodes[i].type == WorldNodeType.Start)
			{
				continue;
			}

			// Sidetrack nodes sit off the main path; skip them.
			if (zones.nodes[i].IsSidetrackNode())
			{
				continue;
			}

			if (fallback < 0)
			{
				fallback = i;
			}

			if (current >= 0 && zones.IsNodeConnected(current, i))
			{
				target = i;
				return true;
			}
		}

		target = fallback;
		return target >= 0;
	}

	public static void Reset()
	{
		_timer = 0f;
		_lastTarget = -1;
	}
}
