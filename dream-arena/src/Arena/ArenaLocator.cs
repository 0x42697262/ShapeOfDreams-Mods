namespace DreamArena.Arena;

internal static class ArenaLocator
{
	public const int NoNode = -1;

	public static Phase CurrentPhase()
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			return Phase.None;
		}

		int index = zones.currentNodeIndex;
		if (index < 0 || index >= zones.nodes.Count)
		{
			return Phase.Camp;
		}

		return zones.nodes[index].type == WorldNodeType.Start ? Phase.Camp : Phase.Arena;
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
			if (i == current || zones.nodes[i].type == WorldNodeType.Start || zones.nodes[i].IsSidetrackNode())
			{
				continue;
			}

			if (fallback == NoNode)
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
		return target != NoNode;
	}
}
