using LastStand.Diagnostics;
using LastStand.Text;

namespace LastStand.Arena;

internal static class ArenaRoomOverride
{
	public static void Apply(string arenaRoom)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null || string.IsNullOrEmpty(arenaRoom))
		{
			return;
		}

		int changed = 0;
		for (int i = 0; i < zones.nodes.Count; i++)
		{
			if (i == zones.currentNodeIndex || !ArenaLocator.IsArenaNode(zones, i)
			                                 || zones.nodes[i].roomOverride == arenaRoom)
			{
				continue;
			}

			zones.SetRoomOverride(i, arenaRoom);
			changed++;
		}

		Log.Info(LogText.ArenaRoomApplied(arenaRoom, changed));
	}
}
