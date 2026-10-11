using System.Collections.Generic;
using UnityEngine;

namespace LastStand.Arena;

internal static class SpawnAnchors
{
	public static string Source => nameof(LavaLand_InfernusPillarPosition);

	public static bool TryFill(List<Vector3> anchors, out Vector3 center)
	{
		anchors.Clear();
		center = Vector3.zero;
		LavaLand_InfernusPillarPosition pillars =
			Object.FindFirstObjectByType<LavaLand_InfernusPillarPosition>(FindObjectsInactive.Include);
		if (pillars == null)
		{
			return false;
		}

		Transform root = pillars.transform;
		center = root.position;
		for (int i = 0; i < root.childCount; i++)
		{
			anchors.Add(root.GetChild(i).position);
		}

		return anchors.Count > 0;
	}
}
