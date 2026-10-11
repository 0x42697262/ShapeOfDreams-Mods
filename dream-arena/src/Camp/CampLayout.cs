using DreamArena.Config;
using UnityEngine;
using UnityEngine.AI;

namespace DreamArena.Camp;

internal static class CampLayout
{
	public static bool TryFind(out Vector3 center, out Vector3 openingDirection)
	{
		center = default;
		openingDirection = Vector3.forward;
		Rift_RoomExit rift = Rift_RoomExit.softInstance;
		if (rift.IsNullOrInactive())
		{
			return false;
		}

		center = Dew.GetPositionOnGround(rift.transform.position);
		if (ArenaLayout.TryFindLargestSection(out Vector3 camp, out float _))
		{
			Vector3 toCamp = camp - center;
			toCamp.y = 0f;
			if (toCamp.sqrMagnitude > Tuning.MinOpeningDirectionSqr)
			{
				openingDirection = toCamp.normalized;
			}
		}

		return true;
	}

	public static bool TryGroundSlot(Vector3 target, out Vector3 slot)
	{
		slot = Dew.GetPositionOnGround(target);
		if (!NavMesh.SamplePosition(slot, out NavMeshHit hit, Tuning.GroundSlotTolerance, NavMesh.AllAreas))
		{
			return false;
		}

		slot.y = hit.position.y;
		return true;
	}

	public static Vector3 Direction(float degrees)
	{
		float radians = degrees * Mathf.Deg2Rad;
		return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
	}

	public static float Angle(Vector3 direction)
	{
		return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
	}
}
