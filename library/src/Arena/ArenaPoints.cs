using UnityEngine;

namespace SodLib.Arena;

internal readonly struct ArenaPoints
{
	public Vector3 Center { get; }

	public float Radius { get; }

	public string Source { get; }

	public ArenaPoints(Vector3 center, float radius, string source)
	{
		Center = center;
		Radius = radius;
		Source = source;
	}
}
