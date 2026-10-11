using System;
using System.Collections.Generic;

namespace SodLib.Arena;

internal static class ArenaRooms
{
	public static void Apply(string arenaRoom, bool stripModifiers)
	{
		Apply(new[] { arenaRoom }, stripModifiers);
	}

	public static void Apply(IReadOnlyList<string> arenaRooms, bool stripModifiers)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			return;
		}

		string room = RoomFor(zones, arenaRooms);
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

	public static void EnsureApplied(string arenaRoom, bool stripModifiers)
	{
		EnsureApplied(new[] { arenaRoom }, stripModifiers);
	}

	public static void EnsureApplied(IReadOnlyList<string> arenaRooms, bool stripModifiers)
	{
		ZoneManager zones = NetworkedManagerBase<ZoneManager>.softInstance;
		if (zones == null)
		{
			return;
		}

		string room = RoomFor(zones, arenaRooms);
		for (int i = 0; i < zones.nodes.Count; i++)
		{
			if (i != zones.currentNodeIndex && ArenaLocator.IsArenaNode(zones, i) && zones.nodes[i].roomOverride != room)
			{
				Log.Warn(SharedLogText.ArenaRoomRestored(room));
				Apply(new[] { room }, stripModifiers);
				return;
			}
		}
	}

	private static string RoomFor(ZoneManager zones, IReadOnlyList<string> arenaRooms)
	{
		List<string> allowed = Allowed(arenaRooms);
		for (int i = 0; i < zones.nodes.Count; i++)
		{
			if (i != zones.currentNodeIndex && ArenaLocator.IsArenaNode(zones, i) &&
			    allowed.Contains(zones.nodes[i].roomOverride))
			{
				return zones.nodes[i].roomOverride;
			}
		}

		string picked = allowed[UnityEngine.Random.Range(0, allowed.Count)];
		if (allowed.Count > 1)
		{
			Log.Info(SharedLogText.ArenaRoomPicked(picked, allowed.Count));
		}

		return picked;
	}

	private static List<string> Allowed(IReadOnlyList<string> arenaRooms)
	{
		List<string> allowed = new List<string>();
		if (arenaRooms != null)
		{
			foreach (string room in arenaRooms)
			{
				if (!string.IsNullOrWhiteSpace(room) && !allowed.Contains(room.Trim()))
				{
					allowed.Add(room.Trim());
				}
			}
		}

		if (allowed.Count == 0)
		{
			allowed.Add(SharedGameNames.DefaultArenaRoom);
		}

		return allowed;
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
