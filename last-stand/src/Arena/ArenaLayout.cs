using LastStand.Config;
using UnityEngine;

namespace LastStand.Arena;

internal static class ArenaLayout
{
	public static bool TryFind(out ArenaPoints points)
	{
		points = default;

		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		if (room == null || !room.isActive)
		{
			return false;
		}

		if (!TryFindCenter(room, out Vector3 center, out float radius))
		{
			return false;
		}

		points = new ArenaPoints(center, radius);
		return true;
	}

	private static bool TryFindCenter(Room room, out Vector3 center, out float radius)
	{
		Room_BossArena marker = SingletonBehaviour<Room_BossArena>.instance;
		if (marker != null)
		{
			center = marker.center;
			radius = marker.radius;
			return true;
		}

		return TryFindLargestSection(room, out center, out radius);
	}

	private static bool TryFindLargestSection(Room room, out Vector3 center, out float radius)
	{
		center = Vector3.zero;
		radius = Tuning.FallbackArenaRadius;

		if (room.sections == null)
		{
			return false;
		}

		RoomSection biggest = null;
		foreach (RoomSection section in room.sections)
		{
			if (section != null && (biggest == null || section.area > biggest.area))
			{
				biggest = section;
			}
		}

		if (biggest == null || biggest.area <= 0f)
		{
			return false;
		}

		center = Dew.GetValidAgentPosition(biggest.pathablePivot);
		radius = Mathf.Clamp(Mathf.Sqrt(biggest.area / Mathf.PI), Tuning.MinArenaRadius, Tuning.MaxArenaRadius);
		return true;
	}
}
