using DreamArena.Config;
using UnityEngine;

namespace DreamArena.Camp;

internal static class RemnantRing
{
	public static bool TrySolve(Vector3 center, Vector3 opening, int count, out Vector3[] slots,
		out Vector3[] facings)
	{
		float openingAngle = CampLayout.Angle(opening);
		foreach (float radius in Tuning.RemnantRingRadii)
		{
			foreach (float rotation in Tuning.RemnantRingRotations)
			{
				if (TryLayOut(center, openingAngle + rotation, radius, count, out slots, out facings))
				{
					return true;
				}
			}
		}

		slots = null;
		facings = null;
		return false;
	}

	private static bool TryLayOut(Vector3 center, float openingAngle, float radius, int count, out Vector3[] slots,
		out Vector3[] facings)
	{
		slots = new Vector3[count];
		facings = new Vector3[count];
		float arc = Tuning.FullCircleDegrees - Tuning.RemnantOpeningDegrees;
		float start = openingAngle + Tuning.HalfCircleDegrees - arc * Tuning.Half;
		float step = arc / (count - 1);
		for (int i = 0; i < count; i++)
		{
			Vector3 direction = CampLayout.Direction(start + step * i);
			if (!CampLayout.TryGroundSlot(center + direction * radius, out slots[i]))
			{
				return false;
			}

			facings[i] = -direction;
		}

		return true;
	}
}
