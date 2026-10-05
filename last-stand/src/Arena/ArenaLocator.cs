namespace LastStand.Arena;

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
}
