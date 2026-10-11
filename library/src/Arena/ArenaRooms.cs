using System;

namespace SodLib.Arena;

internal static class ArenaRooms
{
	public static void Apply(string arenaRoom, bool stripModifiers)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			return;
		}

		string room = string.IsNullOrWhiteSpace(arenaRoom) ? SharedGameNames.DefaultArenaRoom : arenaRoom.Trim();
		try
		{
			int changed = 0;
			int stripped = 0;
			for (int i = 0; i < zones.nodes.Count; i++)
			{
				if (i == zones.currentNodeIndex || !ArenaLocator.IsArenaNode(zones, i))
				{
					continue;
				}

				if (stripModifiers)
				{
					stripped += Strip(zones, i);
				}

				if (zones.nodes[i].roomOverride != room)
				{
					zones.SetRoomOverride(i, room);
					changed++;
				}
			}

			Log.Info(SharedLogText.ArenaRoomApplied(room, changed, stripped));
		}
		catch (Exception e)
		{
			Log.Error(SharedLogText.ArenaRoomFailed(room), e);
		}
	}

	public static int Strip(ZoneManager zones, int nodeIndex)
	{
		if (!ArenaLocator.IsArenaNode(zones, nodeIndex) || zones.nodes[nodeIndex].modifiers == null)
		{
			return 0;
		}

		int[] ids = zones.nodes[nodeIndex].modifiers.ConvertAll(modifier => modifier.id).ToArray();
		foreach (int id in ids)
		{
			zones.RemoveModifier(nodeIndex, id);
		}

		return ids.Length;
	}
}
