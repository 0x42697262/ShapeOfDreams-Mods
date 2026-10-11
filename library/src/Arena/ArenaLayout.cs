using UnityEngine;

namespace SodLib.Arena;

internal static class ArenaLayout
{
	public static bool TryFind(bool useEntryTeleporter, out ArenaPoints points)
	{
		points = default;
		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		if (room == null || !room.isActive)
		{
			return false;
		}

		Room_BossArena marker = SingletonBehaviour<Room_BossArena>.softInstance;
		if (marker != null)
		{
			points = new ArenaPoints(marker.center, marker.radius, nameof(Room_BossArena));
			return true;
		}

		if (useEntryTeleporter && TryFindTeleporterDestination(out Vector3 destination))
		{
			points = new ArenaPoints(destination, SharedTuning.FallbackArenaRadius, nameof(DewHeroesTeleporter));
			return true;
		}

		if (!TryFindLargestSection(out Vector3 center, out float radius))
		{
			return false;
		}

		points = new ArenaPoints(center, radius, nameof(RoomSection));
		return true;
	}

	public static bool TryFindLargestSection(out Vector3 center, out float radius)
	{
		center = Vector3.zero;
		radius = SharedTuning.FallbackArenaRadius;
		Room room = SingletonDewNetworkBehaviour<Room>.softInstance;
		if (room == null || room.sections == null)
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
		radius = Mathf.Clamp(Mathf.Sqrt(biggest.area / Mathf.PI), SharedTuning.MinArenaRadius,
			SharedTuning.MaxArenaRadius);
		return true;
	}

	private static bool TryFindTeleporterDestination(out Vector3 destination)
	{
		destination = Vector3.zero;
		DewHeroesTeleporter teleporter = Object.FindAnyObjectByType<DewHeroesTeleporter>();
		if (teleporter == null || teleporter.destination == null)
		{
			return false;
		}

		destination = Dew.GetPositionOnGround(teleporter.destination.position);
		return true;
	}
}
