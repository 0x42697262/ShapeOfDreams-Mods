using DreamArena.Config;
using UnityEngine;

namespace DreamArena.Arena;

internal static class ArenaLayout
{
	public static bool TryFind(out Vector3 center, out float radius, out string source)
	{
		Room_BossArena marker = SingletonBehaviour<Room_BossArena>.softInstance;
		if (marker != null)
		{
			center = marker.center;
			radius = marker.radius;
			source = nameof(Room_BossArena);
			return true;
		}

		DewHeroesTeleporter teleporter = Object.FindAnyObjectByType<DewHeroesTeleporter>();
		if (teleporter != null && teleporter.destination != null)
		{
			center = Dew.GetPositionOnGround(teleporter.destination.position);
			radius = Tuning.FallbackArenaRadius;
			source = nameof(DewHeroesTeleporter);
			return true;
		}

		source = nameof(RoomSection);
		return TryFindLargestSection(out center, out radius);
	}

	public static bool TryFindLargestSection(out Vector3 center, out float radius)
	{
		center = Vector3.zero;
		radius = Tuning.FallbackArenaRadius;
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
		radius = Mathf.Clamp(Mathf.Sqrt(biggest.area / Mathf.PI), Tuning.MinSectionRadius, Tuning.MaxSectionRadius);
		return true;
	}
}
