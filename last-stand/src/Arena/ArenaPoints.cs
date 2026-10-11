using UnityEngine;

namespace LastStand.Arena;

internal readonly struct ArenaPoints
{
	public Vector3 Center { get; }

	public float Radius { get; }

	public ArenaPoints(Vector3 center, float radius)
	{
		Center = center;
		Radius = radius;
	}
}
