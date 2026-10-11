namespace SodLib.Arena;

internal static class ArenaLocator
{
	public const int NoNode = -1;

	public static bool IsArenaNode(ZoneManager zones, int nodeIndex)
	{
		return zones != null
		       && nodeIndex >= 0
		       && nodeIndex < zones.nodes.Count
		       && zones.nodes[nodeIndex].type != WorldNodeType.Start;
	}

	public static bool IsStandingInArena()
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		return zones != null && !zones.isInAnyTransition && IsArenaNode(zones, zones.currentNodeIndex);
	}

	public static bool IsInArenaNode()
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		return zones != null && IsArenaNode(zones, zones.currentNodeIndex);
	}

	public static int CurrentNode()
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		return zones != null ? zones.currentNodeIndex : NoNode;
	}

	public static ArenaPhase CurrentPhase()
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			return ArenaPhase.None;
		}

		return IsArenaNode(zones, zones.currentNodeIndex) ? ArenaPhase.Arena : ArenaPhase.Camp;
	}

	public static int CampNode(ZoneManager zones)
	{
		for (int i = 0; i < zones.nodes.Count; i++)
		{
			if (zones.nodes[i].type == WorldNodeType.Start)
			{
				return i;
			}
		}

		return NoNode;
	}

	public static bool TryFindArenaNode(ZoneManager zones, out int target)
	{
		int current = zones.currentNodeIndex;
		int fallback = NoNode;
		for (int i = 0; i < zones.nodes.Count; i++)
		{
			if (i == current || !IsArenaNode(zones, i) || zones.nodes[i].IsSidetrackNode())
			{
				continue;
			}

			if (current != NoNode && zones.IsNodeConnected(current, i))
			{
				target = i;
				return true;
			}

			if (fallback == NoNode)
			{
				fallback = i;
			}
		}

		target = fallback;
		return target != NoNode;
	}
}
