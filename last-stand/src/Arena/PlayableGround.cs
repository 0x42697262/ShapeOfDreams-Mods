using LastStand.Config;
using UnityEngine;
using UnityEngine.AI;

namespace LastStand.Arena;

internal static class PlayableGround
{
	private static readonly NavMeshPath Path = new NavMeshPath();

	public static bool IsReachable(Vector3 center, Vector3 spot)
	{
		if (!NavMesh.CalculatePath(center, spot, NavMesh.AllAreas, Path) ||
		    Path.status != NavMeshPathStatus.PathComplete)
		{
			return false;
		}

		float walked = 0f;
		Vector3[] corners = Path.corners;
		for (int i = 1; i < corners.Length; i++)
		{
			walked += Vector3.Distance(corners[i - 1], corners[i]);
		}

		return walked <= Vector3.Distance(center, spot) * Tuning.EdgeMaxPathStretch;
	}

	public static bool IsPlayable(Vector3 position)
	{
		return !Dew.IsNotPlayableOnTop(position);
	}

	public static Vector3 PullToward(Vector3 safe, Vector3 spot)
	{
		Vector3 offset = spot - safe;
		float length = offset.magnitude;
		if (length <= 0f)
		{
			return safe;
		}

		Vector3 direction = offset / length;
		for (float distance = length; distance > 0f; distance -= Tuning.PlayableStepMeters)
		{
			Vector3 candidate = Dew.GetValidAgentDestination_LinearSweep(safe, safe + direction * distance);
			if (IsPlayable(candidate))
			{
				return candidate;
			}
		}

		return safe;
	}
}
